using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Reportes del programa de fidelización para el admin (CU-24).</summary>
[ApiController]
[Route("api/admin/reportes")]
[Authorize(Roles = Roles.Admin)]
public class AdminReportesController(IReporteService reporteService) : ControllerBase
{
    /// <summary> Genera el reporte; tipo inexistente o período inválido devuelven 400 automático (2a). </summary>
    [HttpGet]
    public async Task<IActionResult> Generar([FromQuery] ReporteQueryDTO query) =>
        Ok(await reporteService.GenerarReporteAsync(query));
}
