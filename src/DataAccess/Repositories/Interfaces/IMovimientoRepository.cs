using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Acceso a datos de <see cref="Movimiento"/> (CU-05, CU-07, CU-10, CU-11,
/// CU-20, CU-22, CU-23, CU-26).
/// </summary>
public interface IMovimientoRepository : IRepositorioBase<Movimiento>
{
    /// <summary>Historial completo de un cliente, más reciente primero (CU-05, RN-06).</summary>
    Task<IReadOnlyList<Movimiento>> GetByClienteIdAsync(Guid clienteId);

    /// <summary>
    /// Lotes de acumulación/bono con saldo aún no consumido ni vencido, del
    /// más antiguo al más nuevo (RN-09, FIFO) — la base para calcular saldo
    /// disponible (CU-04, RN-05) y para descontar en un canje (CU-23).
    /// </summary>
    Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia);

    /// <summary>Consulta admin con filtros opcionales (CU-20).</summary>
    Task<IReadOnlyList<Movimiento>> ConsultarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta);

    /// <summary>Existe ya un bono de cumpleaños para ese cliente en el año dado (RN-29).</summary>
    Task<bool> ExisteBonoCumpleanosEnAnioAsync(Guid clienteId, int anio);

    /// <summary>
    /// Todos los lotes (de cualquier cliente) ya vencidos que todavía tienen
    /// saldo sin consumir — lo que el job de vencimiento (CU-22) debe dar de
    /// baja. Con tracking: el job actualiza `PuntosDisponibles` a 0.
    /// </summary>
    Task<IReadOnlyList<Movimiento>> GetLotesVencidosConSaldoAsync(DateTime fechaReferencia);
}
