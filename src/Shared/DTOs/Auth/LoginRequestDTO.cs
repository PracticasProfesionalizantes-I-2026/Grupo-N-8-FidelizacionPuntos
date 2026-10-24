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
    [Required(ErrorMessage = "El email no puede estar vacío.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña no puede estar vacía.")]
    public string Password { get; set; } = string.Empty;
}
