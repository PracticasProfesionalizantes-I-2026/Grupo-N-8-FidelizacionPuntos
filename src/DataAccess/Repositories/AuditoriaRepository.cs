using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IAuditoriaRepository"/>: solo alta y lectura (RN-20).</summary>
public class AuditoriaRepository(FidelixDbContext context) : IAuditoriaRepository
{
    /// <summary>Asigna un nuevo Id y persiste el registro.</summary>
    public async Task<Auditoria> CreateAsync(Auditoria entity)
    {
        entity.Id = Guid.NewGuid();
        context.Auditorias.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Busca un registro por Id, en modo solo lectura.</summary>
    public Task<Auditoria?> GetByIdAsync(Guid id) =>
        context.Auditorias.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>Aplica solo los filtros informados y ordena del más nuevo al más viejo.</summary>
    public async Task<IReadOnlyList<Auditoria>> BuscarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId)
    {
        var query = context.Auditorias.AsNoTracking().AsQueryable();

        if (fechaDesde is not null) query = query.Where(a => a.Fecha >= fechaDesde);
        if (fechaHasta is not null) query = query.Where(a => a.Fecha <= fechaHasta);
        if (!string.IsNullOrWhiteSpace(tipoOperacion)) query = query.Where(a => a.TipoOperacion.Contains(tipoOperacion));
        if (actorId is not null) query = query.Where(a => a.ActorId == actorId);

        return await query.OrderByDescending(a => a.Fecha).ToListAsync();
    }

    /// <summary>Cuenta los registros de un período.</summary>
    public Task<int> ContarAsync(DateTime fechaDesde, DateTime fechaHasta) =>
        context.Auditorias.CountAsync(a => a.Fecha >= fechaDesde && a.Fecha <= fechaHasta);
}
