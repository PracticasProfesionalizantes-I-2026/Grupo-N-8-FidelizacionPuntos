using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Beneficio;

/// <summary>Modificación de un beneficio por el admin (CU-17).</summary>
public class BeneficioUpdateDTO
{
    [MaxLength(100)]
    public string? Nombre { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public int? CostoPuntos { get; set; }

    public CategoriaBeneficio? Categoria { get; set; }
}
