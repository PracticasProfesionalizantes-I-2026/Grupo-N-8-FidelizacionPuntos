using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Empleado;

/// <summary>
/// Alta de un empleado por el admin (CU-16). No incluye contraseña: el
/// empleado la define la primera vez mediante recuperación de contraseña
/// (CU-25), cuyo código se envía al email del admin (RN-28).
/// </summary>
public class EmpleadoCreateDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Documento { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}
