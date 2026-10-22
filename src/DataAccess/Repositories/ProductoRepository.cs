using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary> Implementación de IProductoRepository. </summary>
public class ProductoRepository(FidelixDbContext context) : IProductoRepository
{
    /// <summary> busca un producto por Id </summary>
    public async Task<Producto?> GetByIdAsync(Guid id) =>
        await context.Productos.FirstOrDefaultAsync(p => p.Id == id);

    /// <summary> Obtiene todos los productos. </summary>
    public async Task<IReadOnlyList<Producto>> GetAllAsync() =>
        await context.Productos.AsNoTracking().ToListAsync();

    /// <summary> Obtiene un producto por su nombre </summary>
    public async Task<Producto?> GetByNombreAsync(string nombre) =>
        await context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);

    /// <summary> Crea un nuevo producto. </summary>
    public async Task<Producto> CreateAsync(Producto producto)
    {
        producto.Id = Guid.NewGuid();
        context.Productos.Add(producto);
        await context.SaveChangesAsync();
        return producto;
    }

    /// <summary> Persiste los cambios sobre un producto ya trackeado. </summary>
    public Task UpdateAsync(Producto entity) => context.SaveChangesAsync();
}