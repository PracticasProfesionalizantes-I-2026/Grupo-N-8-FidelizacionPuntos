using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// El empleado registra una compra para acreditarle puntos al cliente (CU-10). El cliente se identifica
/// por su id; el empleado lo obtiene antes buscándolo por DNI (`GET /api/clientes?documento=`).
/// </summary>
public class AcumulacionCreateDTO
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    public Guid? ClienteId { get; set; }

    [Required(ErrorMessage = "La compra debe tener al menos un ítem.")]
    [MinLength(1, ErrorMessage = "La compra debe tener al menos un ítem.")]
    public List<ItemCompraDTO> Items { get; set; } = [];
}
