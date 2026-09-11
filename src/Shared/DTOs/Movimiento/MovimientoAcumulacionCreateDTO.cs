using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Registro de una compra en el punto de venta, para acreditar puntos
/// (CU-10). El empleado que la registra se toma del JWT, no de este DTO.
/// </summary>
public class MovimientoAcumulacionCreateDTO
{
    [Required, MaxLength(20)]
    public string ClienteDocumento { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<ItemCompraDTO> Items { get; set; } = [];
}
