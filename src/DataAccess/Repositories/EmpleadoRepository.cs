using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IEmpleadoRepository"/>.</summary>
public class EmpleadoRepository(FidelixDbContext context) : IEmpleadoRepository
{
    public Task<Empleado?> GetByIdAsync(Guid id) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<Empleado>> GetAllAsync() =>
        await context.Empleados.AsNoTracking().ToListAsync();

    public Task<Empleado?> GetByDocumentoAsync(string documento) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Documento == documento);

    public Task<Empleado?> GetByEmailAsync(string email) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Email == email);

    public async Task<Empleado> CreateAsync(Empleado entity)
    {
        entity.Id = Guid.NewGuid();
        context.Empleados.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public Task UpdateAsync(Empleado entity) => context.SaveChangesAsync();
}
