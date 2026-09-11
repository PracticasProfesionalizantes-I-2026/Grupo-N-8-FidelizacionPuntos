using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Acceso a datos de <see cref="Auditoria"/> (CU-21, CU-24). No extiende
/// <see cref="IRepositorioBase{TEntity}"/>: por RN-20 (inmutabilidad) el
/// registro de auditoría es de solo inserción y consulta, sin `UpdateAsync`.
/// </summary>
public interface IAuditoriaRepository
{
    Task<Auditoria> CreateAsync(Auditoria auditoria);
    Task<Auditoria?> GetByIdAsync(Guid id);

    /// <summary>Consulta con filtros opcionales (CU-21).</summary>
    Task<IReadOnlyList<Auditoria>> ConsultarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId);
}
