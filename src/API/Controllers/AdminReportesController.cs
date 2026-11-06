using FidelixAPI.API.Formatters;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Reportes y comparaciones del programa de fidelización para el admin (CU-24).</summary>
[ApiController]
[Route("api/admin/reportes")]
[Authorize(Roles = Roles.Admin)]
public class AdminReportesController(IReporteService reporteService) : ControllerBase
{
    /// <summary>
    /// Genera el reporte; tipo o formato inexistente, o período inválido, devuelven 400 automático (2a).
    /// Con `formato=Csv` el mismo reporte se entrega como archivo descargable (sub-variación 1).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Generar([FromQuery] ReporteQueryDTO query)
    {
        var reporte = await reporteService.GenerarReporteAsync(query);

        return query.Formato == FormatoReporte.Csv
            ? File(ReporteCsvWriter.Escribir(reporte), "text/csv; charset=utf-8", $"reporte-{reporte.Tipo}-{reporte.PeriodoDesde:yyyyMMdd}-{reporte.PeriodoHasta:yyyyMMdd}.csv")
            : Ok(reporte);
    }

    /// <summary>
    /// Comparación del dashboard: indicadores en un período, o un indicador en dos períodos, día por día.
    /// Parámetros inválidos (período invertido, segundo período sin indicador, etc.) devuelven 400 (2a).
    /// </summary>
    [HttpGet("comparacion")]
    public async Task<IActionResult> Comparar([FromQuery] ComparacionQueryDTO query) =>
        Ok(await reporteService.CompararAsync(query));
}
