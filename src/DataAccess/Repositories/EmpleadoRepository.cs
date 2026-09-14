using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IEmpleadoRepository"/>.</summary>
public class EmpleadoRepository(FidelixDbContext context) : IEmpleadoRepository
{
    /// <summary>Busca un empleado por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Empleado?> GetByIdAsync(Guid id) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Id == id);

    /// <summary>Lista todos los empleados en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Empleado>> GetAllAsync() =>
        await context.Empleados.AsNoTracking().ToListAsync();

    /// <summary>Busca por documento, con tracking.</summary>
    public Task<Empleado?> GetByDocumentoAsync(string documento) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Documento == documento);

    /// <summary>Busca por email, con tracking.</summary>
    public Task<Empleado?> GetByEmailAsync(string email) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Email == email);

    /// <summary>Asigna un nuevo Id y persiste el empleado.</summary>
    public async Task<Empleado> CreateAsync(Empleado entity)
    {
        entity.Id = Guid.NewGuid();
        context.Empleados.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un empleado ya trackeado.</summary>
    public Task UpdateAsync(Empleado entity) => context.SaveChangesAsync();
}
