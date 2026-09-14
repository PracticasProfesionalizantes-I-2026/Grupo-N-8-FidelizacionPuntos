using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IAuditoriaRepository"/> (solo inserción y consulta, RN-20).</summary>
public class AuditoriaRepository(FidelixDbContext context) : IAuditoriaRepository
{
    /// <summary>Asigna un nuevo Id y persiste el registro de auditoría.</summary>
    public async Task<Auditoria> CreateAsync(Auditoria auditoria)
    {
        auditoria.Id = Guid.NewGuid();
        context.Auditorias.Add(auditoria);
        await context.SaveChangesAsync();
        return auditoria;
    }

    /// <summary>Busca un registro por Id, en modo solo lectura (RN-20: nunca se modifica).</summary>
    public Task<Auditoria?> GetByIdAsync(Guid id) =>
        context.Auditorias.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>Aplica los filtros opcionales recibidos y devuelve los registros más recientes primero.</summary>
    public async Task<IReadOnlyList<Auditoria>> ConsultarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId)
    {
        var query = context.Auditorias.AsNoTracking().AsQueryable();

        if (fechaDesde is not null) query = query.Where(a => a.Fecha >= fechaDesde);
        if (fechaHasta is not null) query = query.Where(a => a.Fecha <= fechaHasta);
        if (!string.IsNullOrWhiteSpace(tipoOperacion)) query = query.Where(a => a.TipoOperacion == tipoOperacion);
        if (actorId is not null) query = query.Where(a => a.ActorId == actorId);

        return await query.OrderByDescending(a => a.Fecha).ToListAsync();
    }
}
