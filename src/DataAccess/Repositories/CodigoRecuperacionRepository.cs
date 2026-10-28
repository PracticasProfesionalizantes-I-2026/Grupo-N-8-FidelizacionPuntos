using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="ICodigoRecuperacionRepository"/>.</summary>
public class CodigoRecuperacionRepository(FidelixDbContext context) : ICodigoRecuperacionRepository
{
    /// <summary>Asigna un nuevo Id y persiste el código.</summary>
    public async Task<CodigoRecuperacion> CreateAsync(CodigoRecuperacion entity)
    {
        entity.Id = Guid.NewGuid();
        context.CodigosRecuperacion.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Códigos vigentes del email, del más nuevo al más viejo, con tracking.</summary>
    public async Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorEmailAsync(string email, DateTime ahora) =>
        await context.CodigosRecuperacion
            .Where(c => c.Email == email && !c.Usado && c.FechaExpiracion > ahora)
            .OrderByDescending(c => c.FechaExpiracion)
            .ToListAsync();

    /// <summary>Persiste los cambios sobre un código ya trackeado.</summary>
    public Task UpdateAsync(CodigoRecuperacion entity) => context.SaveChangesAsync();
}
