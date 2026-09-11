using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IProductoRepository"/>.</summary>
public class ProductoRepository(FidelixDbContext context) : IProductoRepository
{
    public Task<Producto?> GetByIdAsync(Guid id) =>
        context.Productos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Producto>> GetAllAsync() =>
        await context.Productos.AsNoTracking().ToListAsync();

    public Task<Producto?> GetByNombreAsync(string nombre) =>
        context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);

    public async Task<Producto> CreateAsync(Producto entity)
    {
        entity.Id = Guid.NewGuid();
        context.Productos.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public Task UpdateAsync(Producto entity) => context.SaveChangesAsync();
}
