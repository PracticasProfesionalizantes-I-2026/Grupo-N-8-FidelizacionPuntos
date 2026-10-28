using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IReglaAcumulacionRepository"/>.</summary>
public class ReglaAcumulacionRepository(FidelixDbContext context) : IReglaAcumulacionRepository
{
    /// <summary>Busca una regla por Id, con tracking (permite modificarla y guardarla después).</summary>
    public Task<ReglaAcumulacion?> GetByIdAsync(Guid id) =>
        context.ReglasAcumulacion.FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Lista todas las versiones de la regla en modo solo lectura.</summary>
    public async Task<IReadOnlyList<ReglaAcumulacion>> GetAllAsync() =>
        await context.ReglasAcumulacion.AsNoTracking().ToListAsync();

    /// <summary>Busca la versión que empieza en esa fecha, en modo solo lectura (solo se usa para comparar).</summary>
    public Task<ReglaAcumulacion?> GetByVigenciaDesdeAsync(DateTime vigenciaDesde) =>
        context.ReglasAcumulacion.AsNoTracking().FirstOrDefaultAsync(r => r.VigenciaDesde == vigenciaDesde);

    /// <summary>Asigna un nuevo Id y persiste la regla.</summary>
    public async Task<ReglaAcumulacion> CreateAsync(ReglaAcumulacion entity)
    {
        entity.Id = Guid.NewGuid();
        context.ReglasAcumulacion.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre una regla ya trackeada.</summary>
    public Task UpdateAsync(ReglaAcumulacion entity) => context.SaveChangesAsync();
}
