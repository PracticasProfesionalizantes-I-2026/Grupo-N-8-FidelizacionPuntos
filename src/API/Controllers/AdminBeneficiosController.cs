using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// ABM de beneficios por el admin (CU-17). Exige el token del login de admin (CU-14, RN-11).
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/beneficios")]
public class AdminBeneficiosController(IBeneficioService beneficioService) : ControllerBase
{
    /// <summary>
    /// Lista todos los beneficios, activos e inactivos, para que el admin vea sus ids y su estado antes
    /// de modificarlos o desactivarlos. Una lista vacía también es 200.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await beneficioService.ListarTodosAsync());

    /// <summary>
    /// Categorías de beneficio válidas (`{ id, nombre }`): el `id` es el número que se envía en
    /// `categoria` al crear o modificar un beneficio.
    /// </summary>
    [HttpGet("categorias")]
    public IActionResult ListarCategorias() => Ok(beneficioService.ListarCategorias());

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
            return BadRequest(new ErrorResponseDTO(ex.Message));
        }
        catch (BeneficioDuplicadoException ex)
        {
            return Conflict(new ErrorResponseDTO(ex.Message));
        }
    }

    /// <summary>Modificación de un beneficio existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, BeneficioUpdateDTO dto)
    {
        try
        {
            var beneficio = await beneficioService.ActualizarAsync(id, dto);
            return Ok(beneficio);
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new ErrorResponseDTO(ex.Message));
        }
        catch (CostoInvalidoException ex)
        {
            return BadRequest(new ErrorResponseDTO(ex.Message));
        }
        catch (BeneficioDuplicadoException ex)
        {
            return Conflict(new ErrorResponseDTO(ex.Message));
        }
    }

    /// <summary>Desactivación lógica (RN-17): el beneficio deja de estar disponible para canje.</summary>
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        try
        {
            var beneficio = await beneficioService.DesactivarAsync(id);
            return Ok(beneficio);
        }
        catch (BeneficioNotFoundException ex)
        {
            return NotFound(new ErrorResponseDTO(ex.Message));
        }
    }
}
