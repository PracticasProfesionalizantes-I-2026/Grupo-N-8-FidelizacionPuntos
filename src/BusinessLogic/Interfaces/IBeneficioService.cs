using FidelixAPI.Shared.DTOs.Beneficio;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Catálogo de beneficios (CU-06) y su ABM por el admin (CU-17).</summary>
public interface IBeneficioService
{
    /// <summary>Catálogo vigente para el cliente (CU-06, RN-17: solo activos).</summary>
    Task<IReadOnlyList<BeneficioResponseDTO>> ConsultarActivosAsync();

    Task<BeneficioResponseDTO> CrearAsync(BeneficioCreateDTO dto);
    Task<BeneficioResponseDTO> ActualizarAsync(Guid id, BeneficioUpdateDTO dto);

    /// <summary>Desactivación lógica (RN-17): saca el beneficio del catálogo.</summary>
    Task<BeneficioResponseDTO> DesactivarAsync(Guid id);
}
