using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Jobs;

/// <summary>
/// Base para los procesos batch de Sistema que corren una vez por día
/// (CU-22, CU-26): ejecuta inmediatamente al arrancar la API y luego cada
/// <see cref="Intervalo"/>, dentro de su propio `IServiceScope` (los
/// services son Scoped; el `BackgroundService` es Singleton). Un error no
/// controlado en una corrida se loguea y no tumba el host — se reintenta en
/// la siguiente corrida.
/// </summary>
public abstract class JobDiarioBase(IServiceScopeFactory scopeFactory, ILogger logger) : BackgroundService
{
    /// <summary>Frecuencia de ejecución; 24 horas por defecto (RF de los CU-22/CU-26: "frecuencia diaria").</summary>
    protected virtual TimeSpan Intervalo => TimeSpan.FromHours(24);

    /// <summary>Nombre descriptivo del job, solo para logging.</summary>
    protected abstract string NombreJob { get; }

    /// <summary>Una corrida del job, con acceso al `IServiceProvider` del scope creado para ella.</summary>
    protected abstract Task EjecutarAsync(IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>Bucle del `BackgroundService`: corre una vez y espera el siguiente tick del timer, hasta que la app se apague.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (!stoppingToken.IsCancellationRequested)
        {
            await EjecutarUnaCorridaAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // La API se está apagando.
            }
        }
    }

    /// <summary>Crea el scope de la corrida y delega en <see cref="EjecutarAsync"/>, conteniendo cualquier error.</summary>
    private async Task EjecutarUnaCorridaAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await EjecutarAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado ejecutando el job {Job}", NombreJob);
        }
    }
}
