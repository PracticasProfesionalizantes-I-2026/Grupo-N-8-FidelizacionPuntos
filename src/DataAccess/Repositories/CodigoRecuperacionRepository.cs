using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="ICodigoRecuperacionRepository"/>.</summary>
public class CodigoRecuperacionRepository(FidelixDbContext context) : ICodigoRecuperacionRepository
{
    public Task<CodigoRecuperacion?> GetByIdAsync(Guid id) =>
        context.CodigosRecuperacion.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<CodigoRecuperacion>> GetAllAsync() =>
        await context.CodigosRecuperacion.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.ClienteId == clienteId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorEmpleadoAsync(Guid empleadoId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.EmpleadoId == empleadoId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorAdminAsync(Guid adminId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.AdminId == adminId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    public async Task<CodigoRecuperacion> CreateAsync(CodigoRecuperacion entity)
    {
        entity.Id = Guid.NewGuid();
        context.CodigosRecuperacion.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public Task UpdateAsync(CodigoRecuperacion entity) => context.SaveChangesAsync();
}
