using FidelixAPI.API.Extensions;
using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FidelixAPI.API.Filters;

/// <summary>
/// Registra en la auditoría (RN-13, CU-21) cada operación que modifica datos (POST, PUT, PATCH,
/// DELETE) y termina bien (2xx): quién la hizo (del token), qué hizo y sobre qué entidad. Al ser
/// un filtro global, ningún controller ni service tiene que acordarse de auditar (principio
/// abierto/cerrado: un endpoint nuevo queda auditado sin tocar este código). No audita los logins
/// ni las consultas (GET), y si la auditoría falla no rompe la respuesta al usuario.
/// </summary>
public class AuditoriaFilter(IAuditoriaService auditoriaService, ILogger<AuditoriaFilter> logger) : IAsyncActionFilter
{
    /// <summary> Verbos HTTP que modifican datos. </summary>
    private static readonly HashSet<string> MetodosAuditados = ["POST", "PUT", "PATCH", "DELETE"];

    /// <summary> Ejecuta la acción y, si modificó datos con éxito, deja el registro de auditoría. </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ejecutado = await next();

        var request = context.HttpContext.Request;
        if (!MetodosAuditados.Contains(request.Method) || ejecutado.Exception is not null)
        {
            return;
        }

        var status = (ejecutado.Result as IStatusCodeActionResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (status is < 200 or >= 300 || context.ActionDescriptor is not ControllerActionDescriptor accion || accion.ActionName.StartsWith("Login"))
        {
            return;
        }

        try
        {
            var user = context.HttpContext.User;
            await auditoriaService.RegistrarAsync(
                $"{accion.ControllerName}.{accion.ActionName}",
                user.ObtenerId(),
                user.ObtenerRol(),
                ObtenerEntidadId(context, ejecutado),
                $"{request.Method} {request.Path} -> {status}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar la auditoría de {Path}", request.Path);
        }
    }

    /// <summary> Id de la entidad afectada: el `{id}` de la ruta o, si no hay, la propiedad `Id` de la respuesta. </summary>
    private static Guid? ObtenerEntidadId(ActionExecutingContext antes, ActionExecutedContext despues)
    {
        if (antes.ActionArguments.TryGetValue("id", out var idRuta) && idRuta is Guid id)
        {
            return id;
        }

        var valor = (despues.Result as ObjectResult)?.Value;
        var propiedad = valor?.GetType().GetProperty("Id") ?? valor?.GetType().GetProperty("MovimientoId");
        return propiedad?.GetValue(valor) as Guid?;
    }
}
