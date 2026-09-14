using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Jobs;

/// <summary>
/// Job programado de CU-22 (Aplicar Vencimiento de Puntos): invoca
/// <see cref="IPuntosService.AplicarVencimientoAsync"/> una vez por día.
/// </summary>
public class VencimientoPuntosJob(IServiceScopeFactory scopeFactory, ILogger<VencimientoPuntosJob> logger)
    : JobDiarioBase(scopeFactory, logger)
{
    protected override string NombreJob => "VencimientoPuntosJob (CU-22)";

    protected override Task EjecutarAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IPuntosService>().AplicarVencimientoAsync();
}
