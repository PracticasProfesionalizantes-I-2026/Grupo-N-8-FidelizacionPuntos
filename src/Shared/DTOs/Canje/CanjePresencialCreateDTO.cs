using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>
/// Cuerpo del canje que registra el empleado en el local (CU-11): solo el beneficio. El cliente NO viaja
/// en el body: su id va como parámetro de ruta (`POST /api/canjes/presencial/{clienteId}`) y el empleado
/// lo obtiene antes buscándolo por DNI (`GET /api/clientes?documento=`).
/// </summary>
public class CanjePresencialCreateDTO
{
    [Required(ErrorMessage = "El beneficio a canjear es obligatorio.")]
    public Guid? BeneficioId { get; set; }
}
