using FidelixAPI.Shared.DTOs.Auth;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Inicio de sesión del personal: empleado (CU-09) y admin (CU-14). Está separado del login del
/// cliente (<see cref="IAuthService"/>, CU-02) porque cada actor está en su propia tabla y el
/// cliente tiene reglas propias (bloqueo por intentos, RN-03).
/// </summary>
public interface IAuthPersonalService
{
    /// <summary>Valida las credenciales del empleado y que esté activo (RN-15); devuelve un token con rol Empleado (RN-11).</summary>
    Task<LoginPersonalResponseDTO> LoginEmpleadoAsync(LoginRequestDTO dto);

    /// <summary>Valida las credenciales del admin; devuelve un token con rol Admin (RN-11).</summary>
    Task<LoginPersonalResponseDTO> LoginAdminAsync(LoginRequestDTO dto);
}
