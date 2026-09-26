using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Movimiento"/> (CU-04).</summary>
public interface IMovimientoRepository : IRepositorioBase<Movimiento>
{
    /// <summary>
    /// Lotes de acumulación/bono con saldo aún no consumido ni vencido, del
    /// más antiguo al más nuevo (RN-09) — la base para calcular el saldo
    /// disponible del cliente (CU-04, RN-05).
    /// </summary>
    Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia);
}
