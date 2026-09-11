using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IPuntosService"/> (CU-04, CU-22, CU-23, CU-26).</summary>
public class PuntosService(
    IMovimientoRepository movimientoRepository,
    IClienteRepository clienteRepository,
    IReglaAcumulacionRepository reglaAcumulacionRepository,
    IAuditoriaService auditoriaService,
    ILogger<PuntosService> logger) : IPuntosService
{
    // CU-26 no especifica el monto; se documenta como constante hasta que exista
    // un lugar de configuración explícito (mismo tratamiento que el umbral de
    // bloqueo de RN-03 en AuthService).
    private const int BonoCumpleanosPuntos = 50;

    /// <summary>Suma el saldo restante de los lotes vigentes (RN-05: ya excluye vencidos).</summary>
    public async Task<SaldoResponseDTO> ConsultarSaldoAsync(Guid clienteId)
    {
        var lotes = await movimientoRepository.GetLotesVigentesPorClienteAsync(clienteId, DateTime.UtcNow);
        return new SaldoResponseDTO { PuntosDisponibles = lotes.Sum(l => l.PuntosDisponibles) };
    }

    /// <summary>Consume lotes del más antiguo al más nuevo (RN-09) hasta cubrir `cantidad`.</summary>
    public async Task DescontarPuntosFifoAsync(Guid clienteId, int cantidad)
    {
        var lotes = await movimientoRepository.GetLotesVigentesPorClienteAsync(clienteId, DateTime.UtcNow);

        if (lotes.Sum(l => l.PuntosDisponibles) < cantidad)
        {
            throw new SaldoInsuficienteException("El cliente no tiene saldo de puntos suficiente para este canje.");
        }

        var restante = cantidad;
        foreach (var lote in lotes)
        {
            if (restante <= 0) break;

            var aConsumir = Math.Min(lote.PuntosDisponibles, restante);
            lote.PuntosDisponibles -= aConsumir;
            restante -= aConsumir;
        }

        try
        {
            // Todos los lotes modificados comparten el mismo DbContext (tracking):
            // una sola llamada persiste el descuento de todos a la vez (CU-23, rollback atómico).
            await movimientoRepository.UpdateAsync(lotes[0]);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo actualizar el saldo de puntos del cliente.", ex);
        }
    }

    /// <summary>Job diario (CU-22): da de baja los lotes vencidos, resiliente a errores por lote.</summary>
    public async Task AplicarVencimientoAsync()
    {
        var lotesVencidos = await movimientoRepository.GetLotesVencidosConSaldoAsync(DateTime.UtcNow);

        foreach (var lote in lotesVencidos)
        {
            try
            {
                var puntosVencidos = lote.PuntosDisponibles;
                lote.PuntosDisponibles = 0;
                await movimientoRepository.UpdateAsync(lote);

                await movimientoRepository.CreateAsync(new Movimiento
                {
                    ClienteId = lote.ClienteId,
                    Tipo = TipoMovimiento.Vencimiento,
                    Puntos = -puntosVencidos,
                    PuntosDisponibles = 0,
                    Detalle = $"Vencimiento del lote {lote.Id}"
                });

                await auditoriaService.RegistrarAsync("AplicarVencimientoPuntos", ActorTipo.Sistema, null, "Cliente", lote.ClienteId, $"{puntosVencidos} puntos vencidos (lote {lote.Id})");
            }
            catch (Exception ex)
            {
                // 4a: un error puntual no interrumpe el batch completo; el lote queda pendiente para el próximo run.
                logger.LogError(ex, "Error al aplicar vencimiento del lote {LoteId}", lote.Id);
            }
        }
    }

    /// <summary>Job diario (CU-26): acredita el bono a los clientes que cumplen años hoy, deduplicado por año (RN-29).</summary>
    public async Task AplicarBonoCumpleanosAsync()
    {
        var hoy = DateTime.UtcNow.Date;
        var reglaActiva = await reglaAcumulacionRepository.GetActivaAsync();
        var diasVigencia = reglaActiva?.DiasVigenciaPuntos ?? 365; // Fallback defensivo si no hay regla activa configurada.

        var clientes = await clienteRepository.GetAllAsync();
        var cumpleaneros = clientes.Where(c => c.Activo && c.FechaNacimiento is { } fn && fn.Day == hoy.Day && fn.Month == hoy.Month);

        foreach (var cliente in cumpleaneros)
        {
            try
            {
                if (await movimientoRepository.ExisteBonoCumpleanosEnAnioAsync(cliente.Id, hoy.Year))
                {
                    continue; // 2a (RN-29): ya recibió el bono este año, se omite sin error.
                }

                await movimientoRepository.CreateAsync(new Movimiento
                {
                    ClienteId = cliente.Id,
                    Tipo = TipoMovimiento.BonoCumpleanos,
                    Puntos = BonoCumpleanosPuntos,
                    PuntosDisponibles = BonoCumpleanosPuntos,
                    FechaVencimiento = DateTime.UtcNow.AddDays(diasVigencia),
                    Detalle = "Bono de cumpleaños"
                });

                await auditoriaService.RegistrarAsync("AplicarBonoCumpleanos", ActorTipo.Sistema, null, "Cliente", cliente.Id, $"{BonoCumpleanosPuntos} puntos de bono");
            }
            catch (Exception ex)
            {
                // 4a: un error puntual no interrumpe el batch completo; el cliente queda pendiente para el próximo run.
                logger.LogError(ex, "Error al aplicar bono de cumpleaños al cliente {ClienteId}", cliente.Id);
            }
        }
    }
}
