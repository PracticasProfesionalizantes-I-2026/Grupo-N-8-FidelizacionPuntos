using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Auth;

/// <summary>
/// Respuesta de un login exitoso: el JWT y el rol bajo el cual se emitió,
/// para que el front sepa qué pantalla mostrar.
/// </summary>
public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public DateTime ExpiraEn { get; set; }
}
