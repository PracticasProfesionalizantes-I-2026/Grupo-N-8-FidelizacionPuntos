using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary> El admin actualiza un producto (CU-18). </summary>
public class ProductoUpdateDTO
{
    [MaxLength(150)]
    public string? Nombre { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public decimal? Precio { get; set; }
}