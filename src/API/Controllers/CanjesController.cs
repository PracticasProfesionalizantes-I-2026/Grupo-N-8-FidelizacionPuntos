using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Canje de beneficios: por el cliente desde la app (CU-07) o por el empleado en el local (CU-11).
/// Cada acción exige el token del rol que le corresponde: sin token responde 401 y con el rol
/// equivocado 403, antes de entrar al método (RN-11).
/// </summary>
[ApiController]
[Route("api/canjes")]
public class CanjesController(ICanjeService canjeService) : ControllerBase
{
    /// <summary> CU-07: canjea con los puntos del cliente del token (RN-08, RN-09, RN-17). </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> CrearCanje(CanjeCreateDTO dto)
    {
        // El id del cliente sale del token (claim NameIdentifier que pone JwtTokenService), nunca del JSON.
        if (User.ObtenerId() is not { } clienteId)
        {
            return Unauthorized();
        }

        return await EjecutarAsync(() => canjeService.CrearCanjeAsync(clienteId, dto));
    }

    /// <summary> CU-11: el empleado canjea por un cliente identificado por documento; queda "Entregado". </summary>
    [HttpPost("presencial")]
    [Authorize(Roles = Roles.Empleado)]
    public async Task<IActionResult> CrearCanjePresencial(CanjePresencialCreateDTO dto) =>
        await EjecutarAsync(() => canjeService.CrearCanjePresencialAsync(dto, User.ObtenerId()));

    /// <summary> RN-10: el empleado entrega en el local un canje "Pendiente" hecho desde la app (CU-11). </summary>
    [HttpPatch("{id:guid}/entregar")]
    [Authorize(Roles = Roles.Empleado)]
    public async Task<IActionResult> MarcarEntregado(Guid id)
    {
        try
        {
            return Ok(await canjeService.MarcarEntregadoAsync(id, User.ObtenerId()));
        }
        catch (CanjeNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CanjeYaEntregadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary> Traduce el resultado o las excepciones del canje a códigos HTTP (común a CU-07 y CU-11). </summary>
    private async Task<IActionResult> EjecutarAsync(Func<Task<CanjeResponseDTO>> canjear)
    {
        try
        {
            var canje = await canjear();
            return StatusCode(StatusCodes.Status201Created, canje);
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (BeneficioInactivoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (SaldoInsuficienteException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
