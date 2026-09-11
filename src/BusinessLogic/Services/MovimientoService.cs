using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IMovimientoService"/> (CU-05, CU-10, CU-20).</summary>
public class MovimientoService(
    IMovimientoRepository movimientoRepository,
    IClienteRepository clienteRepository,
    IReglaAcumulacionRepository reglaAcumulacionRepository,
    IAuditoriaService auditoriaService) : IMovimientoService
{
    /// <summary>
    /// Registra la compra y acredita los puntos según la regla vigente
    /// (RN-12). `dto.Items[].Monto` se interpreta como el importe total de
    /// esa línea (no un precio unitario a multiplicar).
    /// </summary>
    public async Task<MovimientoResponseDTO> RegistrarAcumulacionAsync(Guid empleadoId, MovimientoAcumulacionCreateDTO dto)
    {
        foreach (var item in dto.Items)
        {
            if (item.Cantidad <= 0 || item.Monto <= 0)
            {
                throw new ValidationException($"El ítem '{item.Producto}' tiene una cantidad o un monto inválido.");
            }
        }

        var cliente = await clienteRepository.GetByDocumentoAsync(dto.ClienteDocumento)
            ?? throw new ClienteNotFoundException("El documento ingresado no se encuentra registrado.");

        if (!cliente.Activo)
        {
            // RN-14 (referenciada desde CU-03): un cliente dado de baja no puede acumular puntos.
            throw new CuentaInactivaException("El cliente está dado de baja y no puede acumular puntos.");
        }

        var reglaActiva = await reglaAcumulacionRepository.GetActivaAsync()
            ?? throw new ValidationException("No hay una regla de acumulación activa configurada.");

        var montoTotal = dto.Items.Sum(i => i.Monto);
        var puntos = (int)Math.Floor(montoTotal * reglaActiva.PuntosPorMonto);

        Movimiento movimiento;
        try
        {
            movimiento = await movimientoRepository.CreateAsync(new Movimiento
            {
                ClienteId = cliente.Id,
                Tipo = TipoMovimiento.Acumulacion,
                Puntos = puntos,
                PuntosDisponibles = puntos,
                FechaVencimiento = DateTime.UtcNow.AddDays(reglaActiva.DiasVigenciaPuntos),
                EmpleadoId = empleadoId,
                Detalle = $"Compra por {montoTotal:0.00} ({dto.Items.Count} ítem(s))"
            });
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo registrar la acumulación de puntos.", ex);
        }

        await auditoriaService.RegistrarAsync("RegistrarAcumulacion", ActorTipo.Empleado, empleadoId, "Cliente", cliente.Id, $"{puntos} puntos acreditados");
        return MapToResponseDTO(movimiento);
    }

    /// <summary>Historial completo de un cliente, del más reciente al más antiguo (RN-06).</summary>
    public async Task<IReadOnlyList<MovimientoResponseDTO>> ConsultarHistorialClienteAsync(Guid clienteId)
    {
        var movimientos = await movimientoRepository.GetByClienteIdAsync(clienteId);
        return movimientos.Select(MapToResponseDTO).ToList();
    }

    /// <summary>Consulta admin con filtros opcionales (CU-20, RN-06: incluye acumulaciones y canjes).</summary>
    public async Task<IReadOnlyList<MovimientoResponseDTO>> ConsultarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var movimientos = await movimientoRepository.ConsultarAsync(clienteId, tipo, fechaDesde, fechaHasta);
        return movimientos.Select(MapToResponseDTO).ToList();
    }

    private static MovimientoResponseDTO MapToResponseDTO(Movimiento m) => new()
    {
        Id = m.Id,
        ClienteId = m.ClienteId,
        Tipo = m.Tipo,
        Puntos = m.Puntos,
        Fecha = m.Fecha,
        FechaVencimiento = m.FechaVencimiento,
        EmpleadoId = m.EmpleadoId,
        Detalle = m.Detalle
    };
}
