using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Catálogo de productos para el empleado (CU-18, consulta): de acá saca el `productoId`
/// que manda al registrar una compra (CU-10). Exige el token del login de empleado (CU-09).
/// </summary>
[ApiController]
[Route("api/productos")]
[Authorize(Roles = Roles.Empleado)]
public class ProductosController(IProductoService productoService) : ControllerBase
{
    /// <summary> Lista los productos activos (RN-26); una lista vacía también es 200. </summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await productoService.ListarProductosActivosAsync());
}
