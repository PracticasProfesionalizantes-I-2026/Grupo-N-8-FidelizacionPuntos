using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>Primer paso de la recuperación de contraseña (CU-25): el email y el rol de la cuenta.</summary>
public class RecuperacionSolicitudDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Rol de la cuenta a recuperar ("Cliente", "Empleado" o "Admin"); lo manda la pantalla de login
    /// desde la que se pidió. El email es único dentro de cada rol, no entre roles: un empleado puede
    /// ser también cliente con el mismo email, y sin el rol no habría forma de saber cuál recuperar (CRIT-06).
    /// </summary>
    [Required(ErrorMessage = "El rol es obligatorio.")]
    [RegularExpression("^(Cliente|Empleado|Admin)$", ErrorMessage = "El rol debe ser Cliente, Empleado o Admin.")]
    public string Rol { get; set; } = string.Empty;
}
