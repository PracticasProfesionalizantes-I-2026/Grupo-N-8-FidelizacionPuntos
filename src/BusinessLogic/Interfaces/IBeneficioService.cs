using FidelixAPI.Shared.DTOs.Beneficio;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de beneficios por el admin (CU-17).</summary>
public interface IBeneficioService
{
    /// <summary>Alta de un beneficio: nombre único (RN-23) y costo en puntos positivo (RN-16).</summary>
    Task<BeneficioResponseDTO> CrearAsync(BeneficioCreateDTO dto);

    /// <summary>Modificación parcial de un beneficio existente (solo los campos informados).</summary>
    Task<BeneficioResponseDTO> ActualizarAsync(Guid id, BeneficioUpdateDTO dto);

    /// <summary>Desactivación lógica (RN-17): el beneficio deja de estar disponible para canje.</summary>
    Task<BeneficioResponseDTO> DesactivarAsync(Guid id);
}
