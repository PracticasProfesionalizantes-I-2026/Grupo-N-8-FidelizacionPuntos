using FidelixAPI.Shared.DTOs.ReglaAcumulacion;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Consulta y configuración de la regla de acumulación de puntos por el admin (CU-19).</summary>
public interface IReglaAcumulacionService
{
    /// <summary>Lista todas las versiones de la regla, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<ReglaAcumulacionResponseDTO>> ObtenerReglasAcumulacionAsync();

    /// <summary>Devuelve el detalle de una versión. Lanza 404 si el id no existe (flujo 3c).</summary>
    Task<ReglaAcumulacionResponseDTO> ObtenerReglaAcumulacionPorIdAsync(Guid id);

    /// <summary>Da de alta una versión nueva: valores positivos y sin superponerse con otra (RN-18); registra la fecha de alta (RN-13).</summary>
    Task<ReglaAcumulacionResponseDTO> CrearReglaAcumulacionAsync(ReglaAcumulacionCreateDTO dto);

    /// <summary>Modificación parcial de una versión: mismas validaciones que el alta; registra la fecha de modificación (RN-13).</summary>
    Task<ReglaAcumulacionResponseDTO> ActualizarReglaAcumulacionAsync(Guid id, ReglaAcumulacionUpdateDTO dto);
}
