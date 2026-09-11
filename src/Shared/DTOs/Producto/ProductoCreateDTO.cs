using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary>Alta de un producto por el admin (CU-18).</summary>
public class ProductoCreateDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }
}
