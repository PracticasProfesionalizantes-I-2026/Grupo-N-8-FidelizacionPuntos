using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IReglaAcumulacionRepository"/>.</summary>
public class ReglaAcumulacionRepository(FidelixDbContext context) : IReglaAcumulacionRepository
{
    public Task<ReglaAcumulacion?> GetByIdAsync(Guid id) =>
        context.ReglasAcumulacion.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IReadOnlyList<ReglaAcumulacion>> GetAllAsync() =>
        await context.ReglasAcumulacion.AsNoTracking().ToListAsync();

    /// <summary>La regla activa vigente hoy (fecha de vigencia sin cerrar o que todavía no venció).</summary>
    public Task<ReglaAcumulacion?> GetActivaAsync()
    {
        var hoy = DateTime.UtcNow;
        return context.ReglasAcumulacion
            .FirstOrDefaultAsync(r => r.Activa && r.VigenciaDesde <= hoy && (r.VigenciaHasta == null || r.VigenciaHasta >= hoy));
    }

    /// <summary>Reglas activas cuyo rango de vigencia se cruza con el indicado (RN-18).</summary>
    public async Task<IReadOnlyList<ReglaAcumulacion>> GetActivasSolapadasAsync(DateTime vigenciaDesde, DateTime? vigenciaHasta, Guid? idAExcluir)
    {
        var query = context.ReglasAcumulacion.AsNoTracking().Where(r => r.Activa);
        if (idAExcluir is not null)
        {
            query = query.Where(r => r.Id != idAExcluir);
        }

        // Dos rangos [A, B) y [C, D) se solapan si A < D (o D es abierto) y C < B (o B es abierto).
        return await query
            .Where(r => (vigenciaHasta == null || r.VigenciaDesde <= vigenciaHasta)
                     && (r.VigenciaHasta == null || vigenciaDesde <= r.VigenciaHasta))
            .ToListAsync();
    }

    public async Task<ReglaAcumulacion> CreateAsync(ReglaAcumulacion entity)
    {
        entity.Id = Guid.NewGuid();
        context.ReglasAcumulacion.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public Task UpdateAsync(ReglaAcumulacion entity) => context.SaveChangesAsync();
}
