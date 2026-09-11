using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Canje de beneficios: el propio cliente (CU-07) o un empleado de forma presencial (CU-11).</summary>
[ApiController]
[Route("api/canjes")]
[Authorize(Roles = "Cliente,Empleado")]
public class CanjesController(ICanjeService canjeService) : ControllerBase
{
    /// <summary>
    /// Mismo endpoint para CU-07 y CU-11: el rol del JWT decide si el canje es
    /// propio (Cliente) o presencial para un cliente puntual (Empleado, que
    /// debe indicar `clienteDocumento` en el body).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear(CanjeCreateDTO dto)
    {
        try
        {
            var canje = User.IsInRole("Empleado")
                ? await canjeService.CrearCanjePresencialAsync(User.ObtenerId(), dto)
                : await canjeService.CrearCanjeAsync(User.ObtenerId(), dto);

            return StatusCode(StatusCodes.Status201Created, canje);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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
