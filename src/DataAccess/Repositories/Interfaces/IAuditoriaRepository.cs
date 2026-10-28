using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Acceso a datos de <see cref="Auditoria"/> (RN-13, CU-21). A propósito NO hereda de
/// <see cref="IRepositorioBase{TEntity}"/>: no expone `UpdateAsync`, porque los registros de
/// auditoría son inmutables (RN-20). Solo se pueden crear y consultar.
/// </summary>
public interface IAuditoriaRepository
{
    /// <summary>Genera el Id y persiste un registro nuevo.</summary>
    Task<Auditoria> CreateAsync(Auditoria entity);

    /// <summary>Busca un registro por Id, en modo solo lectura.</summary>
    Task<Auditoria?> GetByIdAsync(Guid id);

    /// <summary>Registros que cumplen todos los filtros informados (los `null` no filtran), del más nuevo al más viejo.</summary>
    Task<IReadOnlyList<Auditoria>> BuscarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId);

    /// <summary>Cantidad de registros en un período (lo usa el reporte para mostrar su consistencia, RN-19).</summary>
    Task<int> ContarAsync(DateTime fechaDesde, DateTime fechaHasta);
}
