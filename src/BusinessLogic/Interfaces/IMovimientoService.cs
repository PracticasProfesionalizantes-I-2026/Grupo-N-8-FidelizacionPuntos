using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Registro de movimientos de puntos por el empleado (CU-10).</summary>
public interface IMovimientoService
{
    /// <summary>
    /// Registra una compra y acredita los puntos según la regla vigente (RN-12), con el
    /// multiplicador Gold (N1) y como pendientes por 15 días (N2); queda auditada (RN-13).
    /// </summary>
    Task<AcumulacionResponseDTO> RegistrarAcumulacionAsync(AcumulacionCreateDTO dto);
}
