using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Login del personal (CU-09 empleado, CU-14 admin) y recuperación de contraseña de cualquier
/// actor (CU-25). Comparte la ruta base `api/auth` con el login del cliente (`AuthController`, CU-02).
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthPersonalController(IAuthPersonalService authPersonalService, IRecuperacionService recuperacionService) : ControllerBase
{
    /// <summary> Inicia sesión como empleado (CU-09). </summary>
    [HttpPost("empleados/login")]
    public async Task<IActionResult> LoginEmpleado(LoginRequestDTO dto)
    {
        try
        {
            return Ok(await authPersonalService.LoginEmpleadoAsync(dto));
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary> Inicia sesión como admin (CU-14). </summary>
    [HttpPost("admin/login")]
    public async Task<IActionResult> LoginAdmin(LoginRequestDTO dto)
    {
        try
        {
            return Ok(await authPersonalService.LoginAdminAsync(dto));
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary> Paso 1 de la recuperación (CU-25): siempre 200, exista o no la cuenta (flujo 3a). </summary>
    [HttpPost("recuperar-contrasena")]
    public async Task<IActionResult> SolicitarRecuperacion(RecuperacionSolicitudDTO dto) =>
        Ok(await recuperacionService.SolicitarRecuperacionAsync(dto));

    /// <summary> Paso 2 de la recuperación (CU-25): código válido (5a) y contraseña válida (5b). </summary>
    [HttpPost("recuperar-contrasena/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacion(RecuperacionConfirmacionDTO dto)
    {
        try
        {
            return Ok(await recuperacionService.ConfirmarRecuperacionAsync(dto));
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
