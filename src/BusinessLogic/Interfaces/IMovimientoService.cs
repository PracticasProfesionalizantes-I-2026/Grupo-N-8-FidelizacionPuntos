using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Registro de movimientos por el empleado (CU-10) y consulta por el admin (CU-20).</summary>
public interface IMovimientoService
{
    /// <summary>
    /// Registra una compra y acredita los puntos según la regla vigente (RN-12), con el
    /// multiplicador Gold (N1) y como pendientes por 15 días (N2); queda auditada con el
    /// empleado que la registró (RN-13, RF-28).
    /// </summary>
    Task<AcumulacionResponseDTO> RegistrarAcumulacionAsync(AcumulacionCreateDTO dto, Guid? empleadoId = null);

    /// <summary>Movimientos de todos los clientes con filtros opcionales: acumulaciones y canjes (CU-20, RN-06).</summary>
    Task<ListadoResponseDTO<MovimientoResponseDTO>> ConsultarMovimientosAsync(MovimientoFiltroDTO filtro);
}
