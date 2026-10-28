using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Procesos;

namespace FidelixAPI.API.Jobs;

/// <summary> Proceso diario del bono de cumpleaños (CU-26). </summary>
public class BonoCumpleanosJob(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<BonoCumpleanosJob> logger)
    : ProcesoDiarioJob(scopeFactory, configuration, logger)
{
    /// <summary> Nombre para el log. </summary>
    protected override string Nombre => "BonoCumpleanosJob";

    /// <summary> Acredita el bono a los cumpleañeros del día. </summary>
    protected override Task<ResultadoProcesoDTO> EjecutarAsync(IPuntosService puntosService, DateTime fecha) =>
        puntosService.AplicarBonoCumpleanosAsync(fecha);
}
