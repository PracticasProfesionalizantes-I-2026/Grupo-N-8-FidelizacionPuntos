using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Registro de acumulaciones (CU-10) y consulta de movimientos (CU-05, CU-20).</summary>
public interface IMovimientoService
{
    /// <summary>Registra una compra y acredita los puntos correspondientes según la regla vigente (CU-10, RN-12).</summary>
    Task<MovimientoResponseDTO> RegistrarAcumulacionAsync(Guid empleadoId, MovimientoAcumulacionCreateDTO dto);

    /// <summary>Historial completo de un cliente (CU-05, RN-06).</summary>
    Task<IReadOnlyList<MovimientoResponseDTO>> ConsultarHistorialClienteAsync(Guid clienteId);

    /// <summary>Consulta admin con filtros opcionales (CU-20).</summary>
    Task<IReadOnlyList<MovimientoResponseDTO>> ConsultarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta);
}
