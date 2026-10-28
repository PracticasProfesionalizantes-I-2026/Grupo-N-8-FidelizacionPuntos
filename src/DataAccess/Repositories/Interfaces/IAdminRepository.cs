using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Admin"/> (CU-14, CU-25).</summary>
public interface IAdminRepository : IRepositorioBase<Admin>
{
    /// <summary>Busca por email (login CU-14, recuperación CU-25).</summary>
    Task<Admin?> GetByEmailAsync(string email);
}
