using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary> El admin actualiza un producto (CU-18). </summary>
public class ProductoUpdateDTO
{
    [MaxLength(150)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")]
    public string? Nombre { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public decimal? Precio { get; set; }
}