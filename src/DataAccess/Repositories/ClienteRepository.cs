using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IClienteRepository"/>.</summary>
public class ClienteRepository(FidelixDbContext context) : IClienteRepository
{
    /// <summary>Busca un cliente por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Cliente?> GetByIdAsync(Guid id) =>
        context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    /// <summary>Lista todos los clientes en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Cliente>> GetAllAsync() =>
        await context.Clientes.AsNoTracking().ToListAsync();

    /// <summary>Busca por documento, con tracking.</summary>
    public Task<Cliente?> GetByDocumentoAsync(string documento) =>
        context.Clientes.FirstOrDefaultAsync(c => c.Documento == documento);

    /// <summary>Busca por email, con tracking.</summary>
    public Task<Cliente?> GetByEmailAsync(string email) =>
        context.Clientes.FirstOrDefaultAsync(c => c.Email == email);

    /// <summary>Asigna un nuevo Id y persiste el cliente.</summary>
    public async Task<Cliente> CreateAsync(Cliente entity)
    {
        entity.Id = Guid.NewGuid();
        context.Clientes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un cliente ya trackeado.</summary>
    public Task UpdateAsync(Cliente entity) => context.SaveChangesAsync();
}
