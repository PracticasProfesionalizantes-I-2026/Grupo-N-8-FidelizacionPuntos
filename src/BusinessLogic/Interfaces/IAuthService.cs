using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Interfaz del servicio de autenticación de clientes (CU-02)</summary>
public interface IAuthService
{
    /// <summary>CU-02: inicia sesión como cliente</summary>
    /// <exception cref="CredencialesInvalidasException">CU-02, flujo 3a.</exception>
    /// <exception cref="CuentaBloqueadaException">CU-02, flujo 3b.</exception>
    /// <exception cref="CuentaInactivaException">CU-02, flujo 3c.</exception>
    Task<LoginResponseDTO> LoginClienteAsync(LoginRequestDTO dto);
}
