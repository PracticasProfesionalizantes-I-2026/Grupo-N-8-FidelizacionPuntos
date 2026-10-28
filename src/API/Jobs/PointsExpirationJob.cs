using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Procesos;

namespace FidelixAPI.API.Jobs;

/// <summary> Proceso diario de vencimiento de puntos (CU-22). </summary>
public class PointsExpirationJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PointsExpirationJob> logger)
    : ProcesoDiarioJob(scopeFactory, configuration, logger)
{
    /// <summary> Nombre para el log. </summary>
    protected override string Nombre => "PointsExpirationJob";

    /// <summary> Aplica el vencimiento de los lotes vencidos a la fecha. </summary>
    protected override Task<ResultadoProcesoDTO> EjecutarAsync(IPuntosService puntosService, DateTime fecha) =>
        puntosService.AplicarVencimientoAsync(fecha);
}
