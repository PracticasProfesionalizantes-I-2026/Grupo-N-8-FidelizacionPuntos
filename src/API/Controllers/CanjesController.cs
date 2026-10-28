using System.Security.Claims;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Canje de beneficios por el cliente (CU-07). Exige el token JWT que devuelve el login
/// (CU-02) con rol "Cliente": sin token responde 401 antes de entrar al método.
/// </summary>
[ApiController]
[Route("api/canjes")]
[Authorize(Roles = "Cliente")]
public class CanjesController(ICanjeService canjeService) : ControllerBase
{
    /// <summary> Canjea un beneficio con los puntos del cliente del token (RN-08, RN-09, RN-17). </summary>
    [HttpPost]
    public async Task<IActionResult> CrearCanje(CanjeCreateDTO dto)
    {
        // El id del cliente sale del token (claim NameIdentifier que pone JwtTokenService), nunca del JSON.
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Unauthorized();
        }

        try
        {
            var canje = await canjeService.CrearCanjeAsync(clienteId, dto);
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
