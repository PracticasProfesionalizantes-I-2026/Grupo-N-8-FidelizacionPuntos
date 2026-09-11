using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Registro de acumulaciones de puntos en el punto de venta (CU-10).</summary>
[ApiController]
[Route("api/movimientos")]
[Authorize(Roles = "Empleado")]
public class MovimientosController(IMovimientoService movimientoService) : ControllerBase
{
    /// <summary>Registra una compra y acredita los puntos correspondientes (CU-10, RN-12).</summary>
    [HttpPost("acumulaciones")]
    public async Task<IActionResult> RegistrarAcumulacion(MovimientoAcumulacionCreateDTO dto)
    {
        try
        {
            var movimiento = await movimientoService.RegistrarAcumulacionAsync(User.ObtenerId(), dto);
            return StatusCode(StatusCodes.Status201Created, movimiento);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
