using FidelixAPI.Shared.DTOs.ReglaAcumulacion;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM y consulta de reglas de acumulación por el admin (CU-19).</summary>
public interface IReglaAcumulacionService
{
    Task<IReadOnlyList<ReglaAcumulacionResponseDTO>> ObtenerTodasAsync();
    Task<ReglaAcumulacionResponseDTO> ObtenerPorIdAsync(Guid id);
    Task<ReglaAcumulacionResponseDTO> CrearAsync(ReglaAcumulacionCreateDTO dto);
    Task<ReglaAcumulacionResponseDTO> ActualizarAsync(Guid id, ReglaAcumulacionUpdateDTO dto);
}
