using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IBeneficioRepository"/>.</summary>
public class BeneficioRepository(FidelixDbContext context) : IBeneficioRepository
{
    public Task<Beneficio?> GetByIdAsync(Guid id) =>
        context.Beneficios.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IReadOnlyList<Beneficio>> GetAllAsync() =>
        await context.Beneficios.AsNoTracking().ToListAsync();

    public Task<Beneficio?> GetByNombreAsync(string nombre) =>
        context.Beneficios.FirstOrDefaultAsync(b => b.Nombre == nombre);

    public async Task<IReadOnlyList<Beneficio>> GetActivosAsync() =>
        await context.Beneficios.AsNoTracking().Where(b => b.Activo).ToListAsync();

    public async Task<Beneficio> CreateAsync(Beneficio entity)
    {
        entity.Id = Guid.NewGuid();
        context.Beneficios.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public Task UpdateAsync(Beneficio entity) => context.SaveChangesAsync();
}
