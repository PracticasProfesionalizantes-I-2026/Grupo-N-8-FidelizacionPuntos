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

    /// <summary>Nueva categoría (opcional); un número que no corresponde a ninguna da 400.</summary>
    [EnumDataType(typeof(CategoriaBeneficio), ErrorMessage = "La categoría no existe. Ver las categorías válidas en GET /api/admin/beneficios/categorias.")]
    public CategoriaBeneficio? Categoria { get; set; }
}
