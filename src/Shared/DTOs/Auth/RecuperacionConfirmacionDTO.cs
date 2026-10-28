using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>Segundo paso de la recuperación (CU-25): el código recibido y la nueva contraseña.</summary>
public class RecuperacionConfirmacionDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código tiene 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Se valida con la política RN-02 en el service (flujo 5b).</summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
