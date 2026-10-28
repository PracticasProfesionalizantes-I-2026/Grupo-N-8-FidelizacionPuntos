using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidelixAPI.API.Controllers;

/// <summary>
/// Ejecución manual de los procesos automáticos (CU-22, CU-26) por el admin: para reintentar
/// después de un "Error parcial" o revisar su resultado sin esperar al próximo día. Los procesos
/// también corren solos una vez por día (`PointsExpirationJob`, `BonoCumpleanosJob`).
/// </summary>
[ApiController]
[Route("api/admin/procesos")]
[Authorize(Roles = Roles.Admin)]
public class AdminProcesosController(IPuntosService puntosService) : ControllerBase
{
    /// <summary> Aplica el vencimiento de puntos ahora (CU-22). </summary>
    [HttpPost("vencimiento-puntos")]
    public async Task<IActionResult> AplicarVencimiento() =>
        Ok(await puntosService.AplicarVencimientoAsync(DateTime.UtcNow));

    /// <summary> Acredita el bono de cumpleaños de hoy (CU-26). </summary>
    [HttpPost("bono-cumpleanos")]
    public async Task<IActionResult> AplicarBonoCumpleanos() =>
        Ok(await puntosService.AplicarBonoCumpleanosAsync(DateTime.UtcNow));
}
