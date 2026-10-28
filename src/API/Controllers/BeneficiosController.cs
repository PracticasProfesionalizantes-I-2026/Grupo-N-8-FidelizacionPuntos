using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>Catálogo de beneficios para el cliente y el empleado (CU-06).</summary>
[ApiController]
[Route("api/beneficios")]
[Authorize(Roles = $"{Roles.Cliente},{Roles.Empleado}")]
public class BeneficiosController(IBeneficioService beneficioService) : ControllerBase
{
    /// <summary> Lista los beneficios activos (RN-07); una lista vacía también es 200 (flujo 4a). </summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await beneficioService.ListarBeneficiosAsync());
}
