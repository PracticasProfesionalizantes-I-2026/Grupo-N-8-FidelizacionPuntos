using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Autenticación unificada (CU-02, CU-09, CU-14) y recuperación de contraseña (CU-25).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Login único para Cliente, Empleado y Admin: el rol se resuelve en `AuthService` (CU-02/CU-09/CU-14).</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        try
        {
            var resultado = await authService.LoginAsync(dto);
            return Ok(resultado);
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

    /// <summary>Solicita el código de recuperación (CU-25, paso 1). Siempre 200: nunca revela si la cuenta existe (RN-27).</summary>
    [HttpPost("recuperar-contrasena")]
    public async Task<IActionResult> SolicitarRecuperacion(RecuperarContrasenaDTO dto)
    {
        await authService.SolicitarRecuperacionAsync(dto);
        return Ok(new { message = "Si el dato ingresado corresponde a una cuenta, se envió un código." });
    }

    /// <summary>Confirma el código y actualiza la contraseña (CU-25, paso 2).</summary>
    [HttpPost("recuperar-contrasena/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacion(ConfirmarRecuperacionDTO dto)
    {
        try
        {
            await authService.ConfirmarRecuperacionAsync(dto);
            return Ok(new { message = "La contraseña se actualizó correctamente." });
        }
        catch (CodigoRecuperacionInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (PasswordInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
