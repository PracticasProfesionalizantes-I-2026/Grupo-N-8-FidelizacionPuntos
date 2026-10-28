using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>Primer paso de la recuperación de contraseña (CU-25): el email de la cuenta.</summary>
public class RecuperacionSolicitudDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;
}
