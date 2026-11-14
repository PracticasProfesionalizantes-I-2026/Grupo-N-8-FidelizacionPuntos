using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="ICodigoRecuperacionRepository"/>.</summary>
public class CodigoRecuperacionRepository(FidelixDbContext context) : ICodigoRecuperacionRepository
{
    /// <summary>Persiste el código nuevo; el Id lo genera la base (autoincremental) y EF lo carga en la entidad.</summary>
    public async Task<CodigoRecuperacion> CreateAsync(CodigoRecuperacion entity)
    {
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

    /// <summary>
    /// Persiste los cambios de los códigos indicados. Se asegura de que todos estén trackeados (por si
    /// alguno llegó sin tracking) y guarda en un único SaveChanges, o sea, en una sola transacción.
    /// </summary>
    public async Task UpdateRangoAsync(IEnumerable<CodigoRecuperacion> codigos)
    {
        context.CodigosRecuperacion.UpdateRange(codigos);
        await context.SaveChangesAsync();
    }
}
