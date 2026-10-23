using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary> El admin da de alta un producto (CU-18). </summary>
public class ProductoCreateDTO
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }
}