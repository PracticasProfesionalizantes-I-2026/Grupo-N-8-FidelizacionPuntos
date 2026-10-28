namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>Respuesta del login de empleado (CU-09) o admin (CU-14): el token y los datos básicos de la cuenta.</summary>
public class LoginPersonalResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
