using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// ABM y consulta de productos por el admin (CU-18). Exige el token del login de admin (CU-14, RN-11).
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/productos")]
public class AdminProductosController(IProductoService productoService) : ControllerBase
{
    /// <summary> Lista todos los productos, activos e inactivos; una lista vacía también es 200. </summary>
    [HttpGet]
    public async Task<IActionResult> ListarProductos() => Ok(await productoService.ListarProductosAsync());

    /// <summary> Alta de un producto (RN-24 y RN-25). </summary>
    [HttpPost]
    public async Task<IActionResult> CrearProducto(ProductoCreateDTO dto)
    {
        try
        {
            var producto = await productoService.CrearProductoAsync(dto);
            return StatusCode(StatusCodes.Status201Created, producto);
        }
        catch (PrecioInvalidoException ex)
        {
            return BadRequest(new ErrorResponseDTO(ex.Message));
        }
        catch (ProductoDuplicadoException ex)
        {
            return Conflict(new ErrorResponseDTO(ex.Message));
        }
    }

    /// <summary> Modificación de un producto existente, actualización parcial (RN-25 y RN-24). </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarProducto(int id, ProductoUpdateDTO dto)
    {
        try
        {
            var producto = await productoService.ActualizarProductoAsync(id, dto);
            return Ok(producto);
        }
        catch (ProductoNotFoundException ex)
        {
            return NotFound(new ErrorResponseDTO(ex.Message));
        }
        catch (PrecioInvalidoException ex)
        {
            return BadRequest(new ErrorResponseDTO(ex.Message));
        }
        catch (ProductoDuplicadoException ex)
        {
            return Conflict(new ErrorResponseDTO(ex.Message));
        }
    }

    /// <summary> Desactivación lógica de un producto (RN-26). </summary>
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> DesactivarProducto(int id)
    {
        try
        {
            var producto = await productoService.DesactivarProductoAsync(id);
            return Ok(producto);
        }
        catch (ProductoNotFoundException ex)
        {
            return NotFound(new ErrorResponseDTO(ex.Message));
        }
    }
}