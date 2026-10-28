using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de ILotePuntosService: el algoritmo FIFO (RN-09). </summary>
public class LotePuntosService : ILotePuntosService
{
    /// <summary>
    /// Recorre los lotes del más antiguo al más nuevo y le saca a cada uno lo que pueda,
    /// hasta cubrir los puntos pedidos. Ej.: lotes [100, 50] y canje de 120 → [0, 30].
    /// </summary>
    public void AplicarFifo(IReadOnlyList<Movimiento> lotes, int puntos)
    {
        var restante = puntos;

        foreach (var lote in lotes.OrderBy(l => l.Fecha))
        {
            if (restante == 0)
            {
                break;
            }

            var descuento = Math.Min(lote.PuntosDisponibles, restante);
            lote.PuntosDisponibles -= descuento;
            restante -= descuento;
        }

        // Defensa: quien llama ya verificó el saldo (RN-08); si igual no alcanza, es un error de programación.
        if (restante > 0)
        {
            throw new InvalidOperationException("Los lotes no alcanzan para cubrir los puntos a descontar.");
        }
    }
}
