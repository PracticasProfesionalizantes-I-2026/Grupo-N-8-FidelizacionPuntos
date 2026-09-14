using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IBeneficioRepository"/>.</summary>
public class BeneficioRepository(FidelixDbContext context) : IBeneficioRepository
{
    /// <summary>Busca un beneficio por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Beneficio?> GetByIdAsync(Guid id) =>
        context.Beneficios.FirstOrDefaultAsync(b => b.Id == id);

    /// <summary>Lista todos los beneficios en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Beneficio>> GetAllAsync() =>
        await context.Beneficios.AsNoTracking().ToListAsync();

    /// <summary>Busca por nombre, con tracking.</summary>
    public Task<Beneficio?> GetByNombreAsync(string nombre) =>
        context.Beneficios.FirstOrDefaultAsync(b => b.Nombre == nombre);

    /// <summary>Lista solo los beneficios activos, en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Beneficio>> GetActivosAsync() =>
        await context.Beneficios.AsNoTracking().Where(b => b.Activo).ToListAsync();

    /// <summary>Asigna un nuevo Id y persiste el beneficio.</summary>
    public async Task<Beneficio> CreateAsync(Beneficio entity)
    {
        entity.Id = Guid.NewGuid();
        context.Beneficios.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un beneficio ya trackeado.</summary>
    public Task UpdateAsync(Beneficio entity) => context.SaveChangesAsync();
}
