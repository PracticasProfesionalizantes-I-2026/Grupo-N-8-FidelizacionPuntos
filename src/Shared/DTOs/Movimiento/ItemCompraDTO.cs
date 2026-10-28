using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Un renglón de la compra (CU-10). El monto no lo manda el empleado: lo calcula el
/// sistema con el precio del producto (CU-18), para que no se pueda acreditar de más.
/// </summary>
public class ItemCompraDTO
{
    [Required(ErrorMessage = "Cada ítem debe indicar el producto.")]
    public Guid? ProductoId { get; set; }

    [Required(ErrorMessage = "Cada ítem debe indicar la cantidad.")]
    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    public int? Cantidad { get; set; }
}
