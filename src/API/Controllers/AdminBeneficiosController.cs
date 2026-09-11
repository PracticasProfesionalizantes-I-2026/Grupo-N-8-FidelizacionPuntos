using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de beneficios por el admin (CU-17).</summary>
[ApiController]
[Route("api/admin/beneficios")]
[Authorize(Roles = "Admin")]
public class AdminBeneficiosController(IBeneficioService beneficioService) : ControllerBase
{
    /// <summary>Alta de un beneficio (RN-16, RN-23).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(BeneficioCreateDTO dto)
    {
        try
        {
            var beneficio = await beneficioService.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, beneficio);
        }
        catch (CostoInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (BeneficioDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Modificación de un beneficio existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, BeneficioUpdateDTO dto)
    {
        try
        {
            var beneficio = await beneficioService.ActualizarAsync(id, dto);
            return Ok(beneficio);
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CostoInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (BeneficioDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Desactivación lógica (RN-17): saca el beneficio del catálogo (CU-06).</summary>
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        try
        {
            var beneficio = await beneficioService.DesactivarAsync(id);
            return Ok(beneficio);
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
