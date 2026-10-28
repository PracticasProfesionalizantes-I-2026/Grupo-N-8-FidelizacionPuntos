using FidelixAPI.Shared.DTOs.Procesos;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Procesos automáticos sobre los puntos: vencimiento (CU-22) y bono de cumpleaños (CU-26).</summary>
public interface IPuntosService
{
    /// <summary>
    /// Vence los lotes cuya fecha de vencimiento ya pasó (FIFO, RN-09), registra un movimiento de
    /// vencimiento por cliente (RN-13) y le avisa. Si falla un cliente, sigue con el resto (4a).
    /// </summary>
    Task<ResultadoProcesoDTO> AplicarVencimientoAsync(DateTime fecha);

    /// <summary>
    /// Acredita el bono a los clientes activos que cumplen años en esa fecha, una sola vez por año
    /// (RN-29). Si falla un cliente, sigue con el resto (4a).
    /// </summary>
    Task<ResultadoProcesoDTO> AplicarBonoCumpleanosAsync(DateTime fecha);
}
