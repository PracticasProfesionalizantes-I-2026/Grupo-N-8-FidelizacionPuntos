using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Consulta y ABM de reglas de acumulación por el admin (CU-19, incluye RF-22).</summary>
[ApiController]
[Route("api/admin/reglas-acumulacion")]
[Authorize(Roles = "Admin")]
public class AdminReglasAcumulacionController(IReglaAcumulacionService reglaAcumulacionService) : ControllerBase
{
    /// <summary>Lista todas las reglas registradas (cierre del gap detectado en el plan técnico).</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var reglas = await reglaAcumulacionService.ObtenerTodasAsync();
        return Ok(reglas);
    }

    /// <summary>Detalle de una regla puntual.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        try
        {
            var regla = await reglaAcumulacionService.ObtenerPorIdAsync(id);
            return Ok(regla);
        }
        catch (ReglaAcumulacionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Alta de una regla (RN-18: sin solapar con una ya activa).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(ReglaAcumulacionCreateDTO dto)
    {
        try
        {
            var regla = await reglaAcumulacionService.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, regla);
        }
        catch (ReglaAcumulacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Modificación de una regla existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ReglaAcumulacionUpdateDTO dto)
    {
        try
        {
            var regla = await reglaAcumulacionService.ActualizarAsync(id, dto);
            return Ok(regla);
        }
        catch (ReglaAcumulacionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ReglaAcumulacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
