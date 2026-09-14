using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IAdminRepository"/>.</summary>
public class AdminRepository(FidelixDbContext context) : IAdminRepository
{
    /// <summary>Busca un admin por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Admin?> GetByIdAsync(Guid id) =>
        context.Admins.FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>Busca por email, con tracking.</summary>
    public Task<Admin?> GetByEmailAsync(string email) =>
        context.Admins.FirstOrDefaultAsync(a => a.Email == email);

    /// <summary>El primer admin activo, en modo solo lectura (RN-28: destino de recuperación de empleados).</summary>
    public Task<Admin?> GetPrimerActivoAsync() =>
        context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Activo);

    /// <summary>Persiste los cambios sobre un admin ya trackeado.</summary>
    public Task UpdateAsync(Admin entity) => context.SaveChangesAsync();
}
