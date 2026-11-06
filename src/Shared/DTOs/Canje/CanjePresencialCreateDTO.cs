using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>
/// El empleado registra un canje en el local (CU-11). El cliente se identifica por su id, que el
/// empleado obtiene buscándolo por DNI (`GET /api/clientes?documento=`).
/// </summary>
public class CanjePresencialCreateDTO
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    public Guid? ClienteId { get; set; }

    [Required(ErrorMessage = "El beneficio a canjear es obligatorio.")]
    public Guid? BeneficioId { get; set; }
}
