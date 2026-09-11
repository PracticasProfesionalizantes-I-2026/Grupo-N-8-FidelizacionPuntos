using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Credenciales de login, comunes a Cliente, Empleado y Admin (CU-02, CU-09,
/// CU-14). El rol se determina en el servicio probando las tres cuentas en
/// cascada, no lo envía el cliente HTTP.
/// </summary>
public class LoginDTO
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
