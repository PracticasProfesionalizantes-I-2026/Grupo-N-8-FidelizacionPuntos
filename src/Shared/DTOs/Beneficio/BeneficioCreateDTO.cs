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

    /// <summary>
    /// Categoría (1 = Descuento, 2 = Producto gratis, 3 = Experiencia, 4 = Otro). Si no se informa, es
    /// "Otro". Un número que no corresponde a ninguna categoría (0, -5, 1234567890) da 400: el JSON de
    /// .NET acepta cualquier entero para un enum, por eso hace falta <see cref="EnumDataTypeAttribute"/>.
    /// </summary>
    [EnumDataType(typeof(CategoriaBeneficio), ErrorMessage = "La categoría no existe. Ver las categorías válidas en GET /api/admin/beneficios/categorias.")]
    public CategoriaBeneficio Categoria { get; set; } = CategoriaBeneficio.Otro;
}
