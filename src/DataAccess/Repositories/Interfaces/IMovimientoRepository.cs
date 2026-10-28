using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.Enums;

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

    /// <summary>
    /// Movimientos que cumplen todos los filtros informados (los `null` no filtran), en orden
    /// cronológico (RN-06), con el beneficio cargado para mostrar su nombre en los canjes.
    /// Solo lectura: lo usan el historial (CU-05), los canjes (CU-08, CU-13), la consulta del
    /// admin (CU-20) y los reportes (CU-24).
    /// </summary>
    Task<IReadOnlyList<Movimiento>> BuscarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta);

    /// <summary>Lotes con puntos sin consumir cuya fecha de vencimiento ya pasó, con tracking (CU-22 los deja en 0).</summary>
    Task<IReadOnlyList<Movimiento>> GetLotesVencidosAsync(DateTime fechaReferencia);

    /// <summary>Indica si el cliente ya recibió el bono de cumpleaños en ese año (RN-29, CU-26).</summary>
    Task<bool> ExisteBonoEnAnioAsync(Guid clienteId, int anio);
}
