using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Ciclo de vida del cliente: autorregistro (CU-01), perfil propio (CU-03),
/// saldo/historial/canjes propios (CU-04/05/08), y las operaciones que un
/// empleado realiza sobre un cliente desde el punto de venta (CU-12, CU-13).
/// </summary>
[ApiController]
[Route("api/clientes")]
public class ClientesController(
    IClienteService clienteService,
    IPuntosService puntosService,
    IMovimientoService movimientoService,
    ICanjeService canjeService) : ControllerBase
{
    /// <summary>Autorregistro (CU-01).</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
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

    /// <summary>Alta desde el punto de venta por un empleado (CU-12).</summary>
    [HttpPost]
    [Authorize(Roles = "Empleado")]
    public async Task<IActionResult> CrearDesdePOS(ClienteCreateDTO dto)
    {
        try
        {
            var cliente = await clienteService.CrearDesdePOSAsync(dto);
            return StatusCode(StatusCodes.Status201Created, cliente);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Ver el perfil propio (CU-03).</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ObtenerPropio()
    {
        var cliente = await clienteService.ObtenerPropioAsync(User.ObtenerId());
        return Ok(cliente);
    }

    /// <summary>Modificar el perfil propio (CU-03).</summary>
    [HttpPut("me")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ActualizarPerfil(ClienteUpdateDTO dto)
    {
        try
        {
            var cliente = await clienteService.ActualizarPerfilAsync(User.ObtenerId(), dto);
            return Ok(cliente);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EmailDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Baja voluntaria de la cuenta (CU-03): baja lógica, sin cuerpo en la respuesta.</summary>
    [HttpDelete("me")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> DarseDeBaja()
    {
        await clienteService.DarseDeBajaAsync(User.ObtenerId());
        return NoContent();
    }

    /// <summary>Saldo de puntos disponible (CU-04).</summary>
    [HttpGet("me/saldo")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ConsultarSaldo()
    {
        var saldo = await puntosService.ConsultarSaldoAsync(User.ObtenerId());
        return Ok(saldo);
    }

    /// <summary>Historial de movimientos propio (CU-05).</summary>
    [HttpGet("me/movimientos")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ConsultarMovimientos()
    {
        var movimientos = await movimientoService.ConsultarHistorialClienteAsync(User.ObtenerId());
        return Ok(movimientos);
    }

    /// <summary>Beneficios canjeados propios (CU-08).</summary>
    [HttpGet("me/canjes")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ConsultarCanjesPropios()
    {
        var canjes = await canjeService.ConsultarPorClienteIdAsync(User.ObtenerId());
        return Ok(canjes);
    }

    /// <summary>Un empleado consulta los canjes de un cliente puntual por documento (CU-13).</summary>
    [HttpGet("{documento}/canjes")]
    [Authorize(Roles = "Empleado")]
    public async Task<IActionResult> ConsultarCanjesPorDocumento(string documento)
    {
        try
        {
            var canjes = await canjeService.ConsultarPorDocumentoAsync(documento);
            return Ok(canjes);
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
