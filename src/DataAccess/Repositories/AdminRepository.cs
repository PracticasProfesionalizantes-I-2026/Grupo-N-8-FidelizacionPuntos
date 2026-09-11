using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IAdminRepository"/>.</summary>
public class AdminRepository(FidelixDbContext context) : IAdminRepository
{
    public Task<Admin?> GetByIdAsync(Guid id) =>
        context.Admins.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Admin?> GetByEmailAsync(string email) =>
        context.Admins.FirstOrDefaultAsync(a => a.Email == email);

    public Task<Admin?> GetPrimerActivoAsync() =>
        context.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Activo);

    public Task UpdateAsync(Admin entity) => context.SaveChangesAsync();
}
