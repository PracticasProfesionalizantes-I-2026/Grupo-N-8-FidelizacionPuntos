using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Generación de reportes agregados por el admin (CU-24).</summary>
[ApiController]
[Route("api/admin/reportes")]
[Authorize(Roles = "Admin")]
public class AdminReportesController(IReporteService reporteService) : ControllerBase
{
    /// <summary>Reporte del tipo y período indicados; 200 aun si no hay datos (4a: resultado vacío válido).</summary>
    [HttpGet]
    public async Task<IActionResult> Generar([FromQuery] TipoReporte tipo, [FromQuery] DateTime periodoDesde, [FromQuery] DateTime periodoHasta)
    {
        try
        {
            var reporte = await reporteService.GenerarReporteAsync(tipo, periodoDesde, periodoHasta);
            return Ok(reporte);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
