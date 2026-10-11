using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Datos para iniciar sesión como cliente (CU-02).
/// Si falta un campo, [ApiController] responde 400 automáticamente (flujo 2a).
/// La contraseña no valida largo mínimo porque esa regla es para crearla
/// (CU-01), no para comprobarla: si la política cambiara, los clientes con
/// contraseñas anteriores no podrían iniciar sesión.
/// </summary>
public class LoginRequestDTO
{
    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
