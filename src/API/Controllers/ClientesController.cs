using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Ciclo de vida del cliente: autorregistro (CU-01), perfil (CU-03), saldo (CU-04), historial (CU-05)
/// y canjes (CU-08) del cliente autenticado (`/me`, el id sale del token), más el alta desde el
/// punto de venta (CU-12) y la consulta de canjes por documento (CU-13) para el empleado.
/// </summary>
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
    /// Saldo de un cliente por Id (CU-04), para el empleado o el admin. El cliente consulta el
    /// suyo con `GET api/clientes/me/saldo`, tomando el Id del token JWT.
    /// </summary>
    [HttpGet("{id:guid}/saldo")]
    [Authorize(Roles = $"{Roles.Empleado},{Roles.Admin}")]
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

    /// <summary> Saldo del cliente autenticado (CU-04). </summary>
    [HttpGet("me/saldo")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> ConsultarMiSaldo()
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        return await ConsultarSaldo(clienteId);
    }

    /// <summary> Perfil del cliente autenticado (CU-03, ver). </summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> ObtenerPerfil()
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        try
        {
            return Ok(await clienteService.ObtenerPerfilAsync(clienteId));
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Modificación parcial del perfil (CU-03): documento no modificable (RN-04), email único (RN-01). </summary>
    [HttpPut("me")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> ActualizarPerfil(ClienteUpdateDTO dto)
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        try
        {
            return Ok(await clienteService.ActualizarPerfilAsync(clienteId, dto));
        }
        catch (DocumentoNoModificableException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (PasswordInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EmailDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Baja voluntaria de la cuenta (CU-03, RN-14): 204 sin contenido. </summary>
    [HttpDelete("me")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> DarseDeBaja()
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        try
        {
            await clienteService.DarseDeBajaAsync(clienteId);
            return NoContent();
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Historial de movimientos con filtros opcionales (CU-05); un filtro mal formado da 400 automático. </summary>
    [HttpGet("me/movimientos")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> ConsultarHistorial([FromQuery] HistorialFiltroDTO filtro)
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        return Ok(await clienteService.ConsultarHistorialAsync(clienteId, filtro));
    }

    /// <summary> Canjes del cliente autenticado con su estado (CU-08). </summary>
    [HttpGet("me/canjes")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<IActionResult> ConsultarCanjes()
    {
        if (User.ObtenerId() is not { } clienteId) return Unauthorized();
        return Ok(await clienteService.ConsultarCanjesAsync(clienteId));
    }

    /// <summary> Alta de un cliente desde el punto de venta, sin contraseña (CU-12, RN-01). </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Empleado)]
    public async Task<IActionResult> CrearDesdePOS(ClienteCreateDTO dto)
    {
        try
        {
            var cliente = await clienteService.CrearClienteDesdePOSAsync(dto);
            return StatusCode(StatusCodes.Status201Created, cliente);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary> Canjes de un cliente buscado por documento, para el empleado (CU-13). </summary>
    [HttpGet("{documento}/canjes")]
    [Authorize(Roles = Roles.Empleado)]
    public async Task<IActionResult> ConsultarCanjesPorDocumento(string documento)
    {
        try
        {
            return Ok(await clienteService.ConsultarCanjesPorDocumentoAsync(documento));
        }
        catch (ClienteNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
