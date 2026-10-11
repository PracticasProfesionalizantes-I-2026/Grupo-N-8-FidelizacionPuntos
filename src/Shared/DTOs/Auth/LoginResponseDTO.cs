using FidelixAPI.Shared.DTOs.Cliente;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Respuesta al iniciar sesión como cliente.
/// CU-02, flujo principal, paso 5.
/// Contiene el token JWT y los datos del cliente.
/// </summary>
public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public ClienteResponseDTO Cliente { get; set; } = new();
}
