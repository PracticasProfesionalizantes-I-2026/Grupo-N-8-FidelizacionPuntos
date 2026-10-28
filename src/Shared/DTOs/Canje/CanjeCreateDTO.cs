using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>
/// El cliente pide canjear un beneficio (CU-07). El cliente no viaja en el JSON: se toma
/// del token JWT, así nadie puede canjear con los puntos de otro.
/// </summary>
public class CanjeCreateDTO
{
    [Required(ErrorMessage = "El beneficio a canjear es obligatorio.")]
    public Guid? BeneficioId { get; set; }
}
