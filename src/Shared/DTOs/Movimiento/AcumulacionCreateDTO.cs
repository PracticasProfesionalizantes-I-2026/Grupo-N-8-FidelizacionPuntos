using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Cuerpo de la compra que registra el empleado (CU-10): solo los ítems. El cliente NO viaja en el body:
/// su id va como parámetro de ruta (`POST /api/movimientos/acumulaciones/{clienteId}`) y el empleado lo
/// obtiene antes buscándolo por DNI (`GET /api/clientes?documento=`).
/// </summary>
public class AcumulacionCreateDTO
{
    [Required(ErrorMessage = "La compra debe tener al menos un ítem.")]
    [MinLength(1, ErrorMessage = "La compra debe tener al menos un ítem.")]
    public List<ItemCompraDTO> Items { get; set; } = [];
}
