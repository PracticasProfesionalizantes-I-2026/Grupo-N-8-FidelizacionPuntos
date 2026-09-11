using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary>Modificación de un producto por el admin (CU-18).</summary>
public class ProductoUpdateDTO
{
    [MaxLength(100)]
    public string? Nombre { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public decimal? Precio { get; set; }
}
