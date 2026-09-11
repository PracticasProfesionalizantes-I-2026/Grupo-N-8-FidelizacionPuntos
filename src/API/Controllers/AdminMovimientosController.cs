using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Consulta de movimientos de puntos por el admin, con filtros opcionales (CU-20).</summary>
[ApiController]
[Route("api/admin/movimientos")]
[Authorize(Roles = "Admin")]
public class AdminMovimientosController(IMovimientoService movimientoService) : ControllerBase
{
    /// <summary>Incluye tanto acumulaciones como canjes (RN-06).</summary>
    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] Guid? clienteId, [FromQuery] TipoMovimiento? tipoMovimiento,
        [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta)
    {
        var movimientos = await movimientoService.ConsultarAsync(clienteId, tipoMovimiento, fechaDesde, fechaHasta);
        return Ok(movimientos);
    }
}
