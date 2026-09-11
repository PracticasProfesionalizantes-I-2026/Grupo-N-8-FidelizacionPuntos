using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>ABM de productos por el admin (CU-18).</summary>
[ApiController]
[Route("api/admin/productos")]
[Authorize(Roles = "Admin")]
public class AdminProductosController(IProductoService productoService) : ControllerBase
{
    /// <summary>Alta de un producto (RN-24, RN-25).</summary>
    [HttpPost]
    public async Task<IActionResult> Crear(ProductoCreateDTO dto)
    {
        try
        {
            var producto = await productoService.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, producto);
        }
        catch (PrecioInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProductoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Modificación de un producto existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ProductoUpdateDTO dto)
    {
        try
        {
            var producto = await productoService.ActualizarAsync(id, dto);
            return Ok(producto);
        }
        catch (ProductoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PrecioInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProductoDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Desactivación lógica (RN-26): el producto deja de estar disponible.</summary>
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        try
        {
            var producto = await productoService.DesactivarAsync(id);
            return Ok(producto);
        }
        catch (ProductoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
