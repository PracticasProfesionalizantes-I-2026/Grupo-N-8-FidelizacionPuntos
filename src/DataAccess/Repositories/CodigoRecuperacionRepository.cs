using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="ICodigoRecuperacionRepository"/>.</summary>
public class CodigoRecuperacionRepository(FidelixDbContext context) : ICodigoRecuperacionRepository
{
    /// <summary>Busca un código por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<CodigoRecuperacion?> GetByIdAsync(Guid id) =>
        context.CodigosRecuperacion.FirstOrDefaultAsync(c => c.Id == id);

    /// <summary>Lista todos los códigos en modo solo lectura.</summary>
    public async Task<IReadOnlyList<CodigoRecuperacion>> GetAllAsync() =>
        await context.CodigosRecuperacion.AsNoTracking().ToListAsync();

    /// <summary>Códigos vigentes (no usados, no vencidos) de un cliente puntual, con tracking (se marcan usados al confirmarse).</summary>
    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.ClienteId == clienteId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    /// <summary>Códigos vigentes de un empleado puntual (RN-28: aunque se envían al email del admin), con tracking.</summary>
    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorEmpleadoAsync(Guid empleadoId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.EmpleadoId == empleadoId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    /// <summary>Códigos vigentes de un admin puntual, con tracking.</summary>
    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorAdminAsync(Guid adminId, DateTime fechaReferencia) =>
        await context.CodigosRecuperacion
            .Where(c => c.AdminId == adminId && !c.Usado && c.FechaExpiracion >= fechaReferencia)
            .ToListAsync();

    /// <summary>Asigna un nuevo Id y persiste el código.</summary>
    public async Task<CodigoRecuperacion> CreateAsync(CodigoRecuperacion entity)
    {
        entity.Id = Guid.NewGuid();
        context.CodigosRecuperacion.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un código ya trackeado (ej. marcarlo como usado).</summary>
    public Task UpdateAsync(CodigoRecuperacion entity) => context.SaveChangesAsync();
}
