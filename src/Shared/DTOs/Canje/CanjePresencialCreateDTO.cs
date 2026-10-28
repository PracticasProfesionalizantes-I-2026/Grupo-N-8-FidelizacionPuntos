using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>El empleado registra un canje en el local, identificando al cliente por su documento (CU-11).</summary>
public class CanjePresencialCreateDTO
{
    [Required(ErrorMessage = "El documento del cliente es obligatorio.")]
    public string ClienteDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El beneficio a canjear es obligatorio.")]
    public Guid? BeneficioId { get; set; }
}
