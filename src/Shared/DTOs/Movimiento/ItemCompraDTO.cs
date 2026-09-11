using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>Un ítem del detalle de una compra registrada en CU-10.</summary>
public class ItemCompraDTO
{
    [Required, MaxLength(100)]
    public string Producto { get; set; } = string.Empty;

    [Required]
    public int Cantidad { get; set; }

    [Required]
    public decimal Monto { get; set; }
}
