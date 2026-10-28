using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de clientes por el admin (CU-15).</summary>
[ApiController]
[Route("api/admin/clientes")]
[Authorize(Roles = Roles.Admin)]
public class AdminClientesController(IClienteAdminService clienteAdminService) : ControllerBase
{
    /// <summary> Lista todos los clientes. </summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await clienteAdminService.ListarClientesAsync());

    /// <summary> Alta de un cliente (RN-01). </summary>
    [HttpPost]
    public async Task<IActionResult> Crear(ClienteCreateDTO dto)
    {
        try
        {
            var cliente = await clienteAdminService.CrearClienteAsync(dto);
            return StatusCode(StatusCodes.Status201Created, cliente);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PasswordInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Modificación parcial de un cliente, incluida la categoría (N1). </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, AdminClienteUpdateDTO dto)
    {
        try
        {
            return Ok(await clienteAdminService.ActualizarClienteAsync(id, dto));
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary> Baja lógica de un cliente (RN-14): 204 sin contenido. </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Baja(Guid id)
    {
        try
        {
            await clienteAdminService.BajaClienteAsync(id);
            return NoContent();
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
