using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de empleados por el admin (CU-16).</summary>
[ApiController]
[Route("api/admin/empleados")]
[Authorize(Roles = "Admin")]
public class AdminEmpleadosController(IEmpleadoAdminService empleadoAdminService) : ControllerBase
{
    /// <summary>Alta de un empleado (sin contraseña: la define vía CU-25, RN-28).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(EmpleadoCreateDTO dto)
    {
        try
        {
            var empleado = await empleadoAdminService.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, empleado);
        }
        catch (EmpleadoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Modificación de los datos de un empleado existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, EmpleadoUpdateDTO dto)
    {
        try
        {
            var empleado = await empleadoAdminService.ActualizarAsync(id, dto);
            return Ok(empleado);
        }
        catch (EmpleadoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (EmpleadoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Baja lógica de un empleado (RN-15: revoca el acceso): sin cuerpo en la respuesta.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DarDeBaja(Guid id)
    {
        try
        {
            await empleadoAdminService.DarDeBajaAsync(id);
            return NoContent();
        }
        catch (EmpleadoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
