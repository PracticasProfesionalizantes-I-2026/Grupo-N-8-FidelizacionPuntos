using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Catálogo de beneficios canjeables (CU-06).</summary>
[ApiController]
[Route("api/beneficios")]
[Authorize] // Cualquier actor autenticado (cliente o empleado) puede ver el catálogo.
public class BeneficiosController(IBeneficioService beneficioService) : ControllerBase
{
    /// <summary>Lista los beneficios activos disponibles para canjear (RN-17).</summary>
    [HttpGet]
    public async Task<IActionResult> ConsultarActivos()
    {
        var beneficios = await beneficioService.ConsultarActivosAsync();
        return Ok(beneficios);
    }
}
