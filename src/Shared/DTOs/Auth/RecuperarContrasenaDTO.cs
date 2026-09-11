using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Solicitud de recuperación de contraseña (CU-25, paso 1). El identificador
/// es el email o documento de la cuenta; el servicio nunca revela si existe
/// o no, para no filtrar información de cuentas registradas (RN-27).
/// </summary>
public class RecuperarContrasenaDTO
{
    [Required]
    public string Identificador { get; set; } = string.Empty;
}
