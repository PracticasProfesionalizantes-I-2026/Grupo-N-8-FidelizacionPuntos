using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de empleados por el admin (CU-16).</summary>
[ApiController]
[Route("api/admin/empleados")]
[Authorize(Roles = Roles.Admin)]
public class AdminEmpleadosController(IEmpleadoAdminService empleadoAdminService) : ControllerBase
{
    /// <summary> Lista todos los empleados. </summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await empleadoAdminService.ListarEmpleadosAsync());

    /// <summary> Alta de un empleado (RN-01, RN-02). </summary>
    [HttpPost]
    public async Task<IActionResult> Crear(EmpleadoCreateDTO dto)
    {
        try
        {
            var empleado = await empleadoAdminService.CrearEmpleadoAsync(dto);
            return StatusCode(StatusCodes.Status201Created, empleado);
        }
        catch (EmpleadoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PasswordInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Modificación parcial de un empleado. </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, EmpleadoUpdateDTO dto)
    {
        try
        {
            return Ok(await empleadoAdminService.ActualizarEmpleadoAsync(id, dto));
        }
        catch (EmpleadoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (EmpleadoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PasswordInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Baja lógica de un empleado (RN-15): pierde el acceso. 204 sin contenido. </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Baja(Guid id)
    {
        try
        {
            await empleadoAdminService.BajaEmpleadoAsync(id);
            return NoContent();
        }
        catch (EmpleadoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
