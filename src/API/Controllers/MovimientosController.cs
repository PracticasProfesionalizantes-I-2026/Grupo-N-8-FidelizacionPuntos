using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Movimientos de puntos registrados por el empleado (CU-10). Todavía sin autenticación:
/// cuando se implemente el login de empleado (CU-09) hay que agregar `[Authorize(Roles = "Empleado")]`
/// y guardar en el movimiento qué empleado lo registró (RN-13, RF-28).
/// </summary>
[ApiController]
[Route("api/movimientos")]
public class MovimientosController(IMovimientoService movimientoService) : ControllerBase
{
    /// <summary> Registra una compra y acredita los puntos (RN-12, N1, N2, RN-13). </summary>
    [HttpPost("acumulaciones")]
    public async Task<IActionResult> RegistrarAcumulacion(AcumulacionCreateDTO dto)
    {
        try
        {
            var acumulacion = await movimientoService.RegistrarAcumulacionAsync(dto);
            return StatusCode(StatusCodes.Status201Created, acumulacion);
        }
        catch (DocumentoInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ItemCompraInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ReglaAcumulacionNoVigenteException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
