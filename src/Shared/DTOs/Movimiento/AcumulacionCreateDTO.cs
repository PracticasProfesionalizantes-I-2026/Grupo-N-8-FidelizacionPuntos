using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>El empleado registra una compra para acreditarle puntos al cliente (CU-10).</summary>
public class AcumulacionCreateDTO
{
    [Required(ErrorMessage = "El documento del cliente es obligatorio.")]
    public string ClienteDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La compra debe tener al menos un ítem.")]
    [MinLength(1, ErrorMessage = "La compra debe tener al menos un ítem.")]
    public List<ItemCompraDTO> Items { get; set; } = [];
}
