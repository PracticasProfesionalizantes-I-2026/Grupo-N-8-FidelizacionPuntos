using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Controlador de autenticación para el inicio de sesión del cliente (CU-02)
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Inicia sesión como cliente (CU-02)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginCliente(LoginRequestDTO dto)
    {
        try
        {
            var login = await authService.LoginClienteAsync(dto);
            return Ok(login);
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (CuentaBloqueadaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}