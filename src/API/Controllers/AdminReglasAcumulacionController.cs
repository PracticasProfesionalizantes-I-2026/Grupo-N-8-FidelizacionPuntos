using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Consulta y configuración de la regla de acumulación por el admin (CU-19). Exige el token del login de admin (CU-14, RN-11).
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/reglas-acumulacion")]
public class AdminReglasAcumulacionController(IReglaAcumulacionService reglaService) : ControllerBase
{
    /// <summary> Lista todas las versiones de la regla. </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerReglas()
    {
        var reglas = await reglaService.ObtenerReglasAcumulacionAsync();
        return Ok(reglas);
    }

    /// <summary> Detalle de una versión (flujo 3c: 404 si no existe). </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerReglaPorId(Guid id)
    {
        try
        {
            var regla = await reglaService.ObtenerReglaAcumulacionPorIdAsync(id);
            return Ok(regla);
        }
        catch (ReglaAcumulacionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Alta de una versión de la regla (RN-18, RN-13). </summary>
    [HttpPost]
    public async Task<IActionResult> CrearRegla(ReglaAcumulacionCreateDTO dto)
    {
        try
        {
            var regla = await reglaService.CrearReglaAcumulacionAsync(dto);
            return StatusCode(StatusCodes.Status201Created, regla);
        }
        catch (ReglaAcumulacionInvalidaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Modificación parcial de una versión existente (RN-18, RN-13). </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> ActualizarRegla(Guid id, ReglaAcumulacionUpdateDTO dto)
    {
        try
        {
            var regla = await reglaService.ActualizarReglaAcumulacionAsync(id, dto);
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
