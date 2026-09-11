using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Consulta de auditoría por el admin (CU-21). Los registros son de solo lectura (RN-20).</summary>
[ApiController]
[Route("api/admin/auditoria")]
[Authorize(Roles = "Admin")]
public class AdminAuditoriaController(IAuditoriaService auditoriaService) : ControllerBase
{
    /// <summary>Consulta con filtros opcionales; sin filtros, igual devuelve resultados (paginación queda para una fase posterior).</summary>
    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta,
        [FromQuery] string? tipoOperacion, [FromQuery] Guid? actorId)
    {
        var registros = await auditoriaService.ConsultarAsync(fechaDesde, fechaHasta, tipoOperacion, actorId);
        return Ok(registros);
    }

    /// <summary>Detalle de una operación puntual.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var registro = await auditoriaService.ObtenerPorIdAsync(id);
        return registro is null ? NotFound() : Ok(registro);
    }
}
