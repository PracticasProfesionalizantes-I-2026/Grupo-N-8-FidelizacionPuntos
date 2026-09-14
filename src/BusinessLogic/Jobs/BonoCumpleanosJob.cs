using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Jobs;

/// <summary>
/// Job programado de CU-26 (Aplicar Bono de Cumpleaños): invoca
/// <see cref="IPuntosService.AplicarBonoCumpleanosAsync"/> una vez por día.
/// </summary>
public class BonoCumpleanosJob(IServiceScopeFactory scopeFactory, ILogger<BonoCumpleanosJob> logger)
    : JobDiarioBase(scopeFactory, logger)
{
    protected override string NombreJob => "BonoCumpleanosJob (CU-26)";

    protected override Task EjecutarAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IPuntosService>().AplicarBonoCumpleanosAsync();
}
