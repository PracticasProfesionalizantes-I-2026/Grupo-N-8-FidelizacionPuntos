using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Confirmación de recuperación de contraseña (CU-25, paso 2): identificador
/// de la cuenta, código recibido por email y la nueva contraseña elegida.
/// </summary>
public class ConfirmarRecuperacionDTO
{
    [Required]
    public string Identificador { get; set; } = string.Empty;

    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    public string NuevaPassword { get; set; } = string.Empty;
}
