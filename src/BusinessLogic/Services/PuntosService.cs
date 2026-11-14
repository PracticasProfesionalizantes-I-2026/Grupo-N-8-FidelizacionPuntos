using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Procesos;
using FidelixAPI.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IPuntosService (CU-22, CU-26). </summary>
public class PuntosService(
    IClienteRepository clienteRepository,
    IMovimientoRepository movimientoRepository,
    IReglaAcumulacionRepository reglaRepository,
    IAuditoriaService auditoriaService,
    IUnidadDeTrabajo unidadDeTrabajo,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<PuntosService> logger) : IPuntosService
{
    /// <summary> Rol con el que se auditan los procesos automáticos (no hay un usuario detrás). </summary>
    private const string ActorSistema = "Sistema";
    /// <summary> Puntos del bono si no se configuró `Puntos:BonoCumpleanos`. </summary>
    private const int BonoPorDefecto = 500;
    /// <summary> Días de vigencia del bono si no hay regla vigente. </summary>
    private const int DiasVigenciaPorDefecto = 180;

    /// <summary>
    /// CU-22: agrupa los lotes vencidos por cliente y, para cada uno, los deja en 0 del más viejo al
    /// más nuevo (RN-09), registra un movimiento negativo de vencimiento y lo audita (RN-13). Si un
    /// cliente falla, se descartan sus cambios pendientes y se sigue con el siguiente (Error parcial, 4a).
    /// </summary>
    public async Task<ResultadoProcesoDTO> AplicarVencimientoAsync(DateTime fecha)
    {
        var lotes = await movimientoRepository.GetLotesVencidosAsync(fecha);
        if (lotes.Count == 0)
        {
            logger.LogInformation("Sin vencimientos a aplicar");
            return Resultado("VencimientoPuntos", 0, 0, 0, "Sin vencimientos a aplicar");
        }

        int procesados = 0, errores = 0, puntosVencidos = 0;

        foreach (var grupo in lotes.GroupBy(l => l.ClienteId))
        {
            var lotesCliente = grupo.OrderBy(l => l.Fecha).ToList();
            var total = lotesCliente.Sum(l => l.PuntosDisponibles);
            Movimiento vencimiento;

            try
            {
                foreach (var lote in lotesCliente)
                {
                    lote.PuntosDisponibles = 0;
                }

                // El SaveChanges de CreateAsync guarda el vencimiento y los lotes en 0: todo o nada.
                vencimiento = await movimientoRepository.CreateAsync(new Movimiento
                {
                    ClienteId = grupo.Key,
                    Tipo = TipoMovimiento.Vencimiento,
                    Puntos = -total,
                    PuntosDisponibles = 0,
                    Fecha = fecha,
                    Detalle = $"Vencimiento de {lotesCliente.Count} lote(s)"
                });
            }
            catch (Exception ex)
            {
                // CRIT-02: el movimiento que falló quedó "Added" y los lotes "Modified" en el contexto.
                // Si no se descartan, el SaveChanges del cliente siguiente los guardaría también.
                unidadDeTrabajo.DescartarCambios();
                errores++;
                logger.LogError(ex, "No se pudo aplicar el vencimiento del cliente {ClienteId}", grupo.Key);
                continue;
            }

            // A partir de acá el vencimiento ya está guardado: cuenta como procesado pase lo que pase.
            procesados++;
            puntosVencidos += total;
            await AuditarProcesoAsync("Proceso.VencimientoPuntos", vencimiento.Id,
                $"Cliente {grupo.Key}: vencieron {total} puntos", grupo.Key);
            await NotificarVencimientoAsync(grupo.Key, total);
        }

        return Resultado("VencimientoPuntos", procesados, errores, puntosVencidos,
            $"Se vencieron {puntosVencidos} puntos de {procesados} cliente(s).");
    }

    /// <summary>
    /// CU-26: busca los clientes activos que cumplen años en la fecha (los nacidos un 29/2 lo
    /// festejan el 28/2 en los años no bisiestos), omite a los que ya recibieron el bono este año
    /// (2a, RN-29) y acredita el bono como un lote disponible de inmediato, con su vencimiento (RN-21).
    /// </summary>
    public async Task<ResultadoProcesoDTO> AplicarBonoCumpleanosAsync(DateTime fecha)
    {
        var cumpleaneros = await BuscarCumpleanerosAsync(fecha);

        if (cumpleaneros.Count == 0)
        {
            return Resultado("BonoCumpleanos", 0, 0, 0, "No hay clientes que cumplan años en la fecha.");
        }

        var bono = int.TryParse(configuration["Puntos:BonoCumpleanos"], out var configurado) ? configurado : BonoPorDefecto;
        var regla = await reglaRepository.GetVigenteAsync(fecha);
        var diasVigencia = regla?.DiasVigenciaPuntos ?? DiasVigenciaPorDefecto;

        int acreditados = 0, omitidos = 0, errores = 0;

        foreach (var cliente in cumpleaneros)
        {
            Movimiento movimiento;
            try
            {
                if (await movimientoRepository.ExisteBonoEnAnioAsync(cliente.Id, fecha.Year))
                {
                    omitidos++;
                    continue;
                }

                movimiento = await movimientoRepository.CreateAsync(new Movimiento
                {
                    ClienteId = cliente.Id,
                    Tipo = TipoMovimiento.BonoCumpleanos,
                    Puntos = bono,
                    PuntosDisponibles = bono,
                    Fecha = fecha,
                    FechaVencimiento = fecha.AddDays(diasVigencia),
                    Detalle = $"Bono de cumpleaños {fecha.Year}"
                });
            }
            catch (Exception ex)
            {
                // CRIT-02: el bono que falló no puede quedar "Added" y guardarse con el cliente siguiente.
                unidadDeTrabajo.DescartarCambios();
                errores++;
                logger.LogError(ex, "No se pudo acreditar el bono del cliente {ClienteId}; se reintenta en la próxima ejecución", cliente.Id);
                continue;
            }

            acreditados++;
            await AuditarProcesoAsync("Proceso.BonoCumpleanos", movimiento.Id, $"Cliente {cliente.Id}: bono de {bono} puntos", cliente.Id);
        }

        return Resultado("BonoCumpleanos", acreditados, errores, acreditados * bono,
            $"Bono acreditado a {acreditados} cliente(s); {omitidos} ya lo habían recibido este año.");
    }

    /// <summary>
    /// Clientes activos que cumplen años en la fecha. El filtro corre en la base (CRIT-03: antes se
    /// traía la tabla completa a memoria). Regla del 29/2: en un año no bisiesto, los nacidos ese día
    /// festejan el 28/2, así que ese día se suma una segunda consulta por 29/2.
    /// </summary>
    private async Task<List<Cliente>> BuscarCumpleanerosAsync(DateTime fecha)
    {
        var cumpleaneros = (await clienteRepository.GetActivosNacidosElAsync(fecha.Month, fecha.Day)).ToList();

        if (fecha.Month == 2 && fecha.Day == 28 && !DateTime.IsLeapYear(fecha.Year))
        {
            cumpleaneros.AddRange(await clienteRepository.GetActivosNacidosElAsync(2, 29));
        }

        return cumpleaneros;
    }

    /// <summary>
    /// Audita un movimiento ya guardado por un proceso (RN-13). Si la auditoría falla NO se deshace el
    /// movimiento (ya está persistido): se descarta el registro de auditoría pendiente, para que no se
    /// guarde con el cliente siguiente, y se registra el error en el log.
    /// </summary>
    private async Task AuditarProcesoAsync(string tipoOperacion, int movimientoId, string detalle, int clienteId)
    {
        try
        {
            await auditoriaService.RegistrarAsync(tipoOperacion, null, ActorSistema, movimientoId, detalle);
        }
        catch (Exception ex)
        {
            unidadDeTrabajo.DescartarCambios();
            logger.LogError(ex, "{Operacion} del cliente {ClienteId} guardado, pero no se pudo auditar", tipoOperacion, clienteId);
        }
    }

    /// <summary> Avisa al cliente por email; si el aviso falla no se deshace el vencimiento, solo se registra. </summary>
    private async Task NotificarVencimientoAsync(int clienteId, int puntos)
    {
        try
        {
            var cliente = await clienteRepository.GetByIdAsync(clienteId);
            if (cliente is not null)
            {
                await emailSender.EnviarAsync(cliente.Email, "Fidelix - Puntos vencidos",
                    $"Hola {cliente.Nombre}, vencieron {puntos} de tus puntos.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo notificar el vencimiento al cliente {ClienteId}", clienteId);
        }
    }

    /// <summary> Arma el resultado del proceso: Éxito, Sin cambios o Error parcial. </summary>
    private static ResultadoProcesoDTO Resultado(string proceso, int procesados, int errores, int puntos, string mensaje) => new()
    {
        Proceso = proceso,
        Estado = errores > 0 ? "ErrorParcial" : procesados > 0 ? "Exito" : "SinCambios",
        ClientesProcesados = procesados,
        ClientesConError = errores,
        PuntosAfectados = puntos,
        Mensaje = mensaje
    };
}
