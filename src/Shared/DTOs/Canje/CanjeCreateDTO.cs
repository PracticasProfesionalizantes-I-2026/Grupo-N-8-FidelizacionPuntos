using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>
/// Solicitud de canje de un beneficio. Se reutiliza en CU-07 (el propio
/// cliente canjea, autenticado: <see cref="ClienteDocumento"/> queda `null`
/// y el cliente se toma del JWT) y en CU-11 (el empleado canjea de forma
/// presencial para un cliente puntual, identificado por documento).
/// </summary>
public class CanjeCreateDTO
{
    [Required]
    public Guid BeneficioId { get; set; }

    /// <summary>Obligatorio solo en el canje presencial hecho por un empleado (CU-11).</summary>
    public string? ClienteDocumento { get; set; }
}
