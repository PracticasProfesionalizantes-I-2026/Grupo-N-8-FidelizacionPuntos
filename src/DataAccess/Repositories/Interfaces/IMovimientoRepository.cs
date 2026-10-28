using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Movimiento"/> (CU-04, CU-07, CU-10).</summary>
public interface IMovimientoRepository : IRepositorioBase<Movimiento>
{
    /// <summary>
    /// Lotes de acumulación/bono con saldo aún no consumido ni vencido y ya
    /// disponibles (no pendientes, N2), del más antiguo al más nuevo (RN-09).
    /// Solo lectura: la base para calcular el saldo disponible (CU-04, RN-05).
    /// </summary>
    Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia);

    /// <summary>
    /// Mismos lotes que <see cref="GetLotesVigentesPorClienteAsync"/>, pero con tracking:
    /// el canje (CU-07) les descuenta puntos por FIFO y los guarda en la misma transacción.
    /// </summary>
    Task<IReadOnlyList<Movimiento>> GetLotesParaCanjeAsync(Guid clienteId, DateTime fechaReferencia);
}
