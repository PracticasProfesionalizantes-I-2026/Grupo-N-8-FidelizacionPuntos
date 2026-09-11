using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de clientes por el admin (CU-15).</summary>
[ApiController]
[Route("api/admin/clientes")]
[Authorize(Roles = "Admin")]
public class AdminClientesController(IClienteAdminService clienteAdminService) : ControllerBase
{
    /// <summary>Alta de un cliente desde el panel de administración.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(ClienteCreateDTO dto)
    {
        try
        {
            var cliente = await clienteAdminService.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, cliente);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Modificación de los datos de un cliente existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ClienteUpdateDTO dto)
    {
        try
        {
            var cliente = await clienteAdminService.ActualizarAsync(id, dto);
            return Ok(cliente);
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

    /// <summary>Baja lógica de un cliente (RN-14): sin cuerpo en la respuesta.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DarDeBaja(Guid id)
    {
        try
        {
            await clienteAdminService.DarDeBajaAsync(id);
            return NoContent();
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
