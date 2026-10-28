using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Procesos;

namespace FidelixAPI.API.Jobs;

/// <summary>
/// Base de los procesos automáticos que corren una vez por día (CU-22, CU-26). Se ejecuta al
/// arrancar la API y después cada 24 horas. Se puede apagar con `Jobs:Habilitados = false`
/// (los tests de integración lo apagan para que no cambien los datos mientras corren).
/// Nunca deja que una excepción tire abajo la API: la registra y espera a la próxima ejecución.
/// </summary>
public abstract class ProcesoDiarioJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger logger) : BackgroundService
{
    /// <summary> Cada cuánto se ejecuta el proceso. </summary>
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    /// <summary> Nombre del proceso, para el log. </summary>
    protected abstract string Nombre { get; }

    /// <summary> Lo que hace el proceso concreto con el service de puntos. </summary>
    protected abstract Task<ResultadoProcesoDTO> EjecutarAsync(IPuntosService puntosService, DateTime fecha);

    /// <summary> Bucle del proceso: ejecuta, registra el resultado y espera 24 horas. </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(configuration["Jobs:Habilitados"], "false", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("{Proceso} deshabilitado por configuración", Nombre);
            return;
        }

        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                // El service es Scoped (usa el DbContext): se crea un scope nuevo en cada ejecución.
                using var scope = scopeFactory.CreateScope();
                var puntosService = scope.ServiceProvider.GetRequiredService<IPuntosService>();
                var resultado = await EjecutarAsync(puntosService, DateTime.UtcNow);
                logger.LogInformation("{Proceso}: {Estado} - {Mensaje}", Nombre, resultado.Estado, resultado.Mensaje);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Proceso} falló; se reintenta en la próxima ejecución", Nombre);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
