using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Consulta de la auditoría de operaciones por el admin (CU-21). Solo lectura (RN-20).</summary>
[ApiController]
[Route("api/admin/auditoria")]
[Authorize(Roles = Roles.Admin)]
public class AdminAuditoriaController(IAuditoriaService auditoriaService) : ControllerBase
{
    /// <summary> Busca registros por criterios; sin criterios devuelve una lista vacía (3a). </summary>
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] AuditoriaFiltroDTO filtro) =>
        Ok(await auditoriaService.ConsultarAuditoriaAsync(filtro));

    /// <summary> Detalle de una operación puntual. </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        try
        {
            return Ok(await auditoriaService.ObtenerPorIdAsync(id));
        }
        catch (AuditoriaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
