using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Cálculo sobre los lotes de puntos de un cliente (CU-23, usado por CU-07).</summary>
public interface ILotePuntosService
{
    /// <summary>
    /// Descuenta <paramref name="puntos"/> de los lotes empezando por el más antiguo (FIFO, RN-09).
    /// Modifica `PuntosDisponibles` de cada lote; no guarda nada (eso lo hace quien lo llama).
    /// </summary>
    void AplicarFifo(IReadOnlyList<Movimiento> lotes, int puntos);
}
