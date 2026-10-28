using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IAdminRepository"/>.</summary>
public class AdminRepository(FidelixDbContext context) : IAdminRepository
{
    /// <summary>Busca un admin por Id, con tracking.</summary>
    public Task<Admin?> GetByIdAsync(Guid id) =>
        context.Admins.FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>Lista todos los admins en modo solo lectura (RN-28: a quién avisar por un empleado).</summary>
    public async Task<IReadOnlyList<Admin>> GetAllAsync() =>
        await context.Admins.AsNoTracking().ToListAsync();

    /// <summary>Busca por email, con tracking (la recuperación puede cambiarle la contraseña).</summary>
    public Task<Admin?> GetByEmailAsync(string email) =>
        context.Admins.FirstOrDefaultAsync(a => a.Email == email);

    /// <summary>Asigna un nuevo Id y persiste el admin.</summary>
    public async Task<Admin> CreateAsync(Admin entity)
    {
        entity.Id = Guid.NewGuid();
        context.Admins.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un admin ya trackeado.</summary>
    public Task UpdateAsync(Admin entity) => context.SaveChangesAsync();
}
