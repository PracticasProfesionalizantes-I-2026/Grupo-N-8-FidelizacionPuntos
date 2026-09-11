using FidelixAPI.Shared.DTOs.Auth;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Autenticación y recuperación de contraseña, unificadas para los tres
/// roles con login (CU-02, CU-09, CU-14, CU-25).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Prueba las credenciales en cascada Cliente → Empleado → Admin y
    /// devuelve un JWT con el rol que matcheó.
    /// </summary>
    Task<LoginResponseDTO> LoginAsync(LoginDTO dto);

    /// <summary>Genera y envía el código de recuperación (CU-25, paso 1). Nunca revela si la cuenta existe.</summary>
    Task SolicitarRecuperacionAsync(RecuperarContrasenaDTO dto);

    /// <summary>Valida el código y actualiza la contraseña (CU-25, paso 2).</summary>
    Task ConfirmarRecuperacionAsync(ConfirmarRecuperacionDTO dto);
}
