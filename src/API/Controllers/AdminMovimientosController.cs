using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Consulta de movimientos de todos los clientes por el admin (CU-20).</summary>
[ApiController]
[Route("api/admin/movimientos")]
[Authorize(Roles = Roles.Admin)]
public class AdminMovimientosController(IMovimientoService movimientoService) : ControllerBase
{
    /// <summary> Movimientos con filtros opcionales; un filtro mal formado devuelve 400 automático (2a). </summary>
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] MovimientoFiltroDTO filtro) =>
        Ok(await movimientoService.ConsultarMovimientosAsync(filtro));
}
