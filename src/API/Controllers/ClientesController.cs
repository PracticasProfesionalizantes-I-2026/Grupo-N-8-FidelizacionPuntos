using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Ciclo de vida del cliente: autorregistro (CU-01) y consulta de saldo (CU-04).</summary>
[ApiController]
[Route("api/clientes")]
public class ClientesController(IClienteService clienteService) : ControllerBase
{
    /// <summary>Autorregistro (CU-01).</summary>
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(ClienteCreateDTO dto)
    {
        try
        {
            var cliente = await clienteService.RegistrarAsync(dto);
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

    /// <summary>
    /// Saldo de puntos disponible (CU-04). Temporalmente recibe el Id del
    /// cliente en la ruta: cuando se implemente el login (CU-02) pasa a ser
    /// `GET api/clientes/me/saldo`, tomando el Id del token JWT.
    /// </summary>
    [HttpGet("{id:guid}/saldo")]
    public async Task<IActionResult> ConsultarSaldo(Guid id)
    {
        try
        {
            var saldo = await clienteService.ConsultarSaldoAsync(id);
            return Ok(saldo);
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
