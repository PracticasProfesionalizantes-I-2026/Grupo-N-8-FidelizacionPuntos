using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IProductoRepository"/>.</summary>
public class ProductoRepository(FidelixDbContext context) : IProductoRepository
{
    /// <summary>Busca un producto por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Producto?> GetByIdAsync(Guid id) =>
        context.Productos.FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>Lista todos los productos en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Producto>> GetAllAsync() =>
        await context.Productos.AsNoTracking().ToListAsync();

    /// <summary>Busca por nombre, con tracking.</summary>
    public Task<Producto?> GetByNombreAsync(string nombre) =>
        context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);

    /// <summary>Asigna un nuevo Id y persiste el producto.</summary>
    public async Task<Producto> CreateAsync(Producto entity)
    {
        entity.Id = Guid.NewGuid();
        context.Productos.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un producto ya trackeado.</summary>
    public Task UpdateAsync(Producto entity) => context.SaveChangesAsync();
}
