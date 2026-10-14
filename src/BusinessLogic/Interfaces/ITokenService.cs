namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Interfaz del servicio que genera un token JWT con los datos del usuario.
/// CU-02, flujo principal paso 4.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un token JWT para un usuario dado su id y rol.
    /// CU-02, flujo principal paso 4.
    /// </summary>
    string GenerarToken(Guid id, string rol);
}