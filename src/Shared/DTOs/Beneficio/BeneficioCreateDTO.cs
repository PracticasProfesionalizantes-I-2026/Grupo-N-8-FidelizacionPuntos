using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Beneficio;

/// <summary>Alta de un beneficio canjeable por el admin (CU-17).</summary>
public class BeneficioCreateDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public int CostoPuntos { get; set; }

    public CategoriaBeneficio Categoria { get; set; } = CategoriaBeneficio.Otro;
}
