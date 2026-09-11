using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Cliente"/> (CU-01, CU-03, CU-12, CU-15).</summary>
public interface IClienteRepository : IRepositorioBase<Cliente>
{
    /// <summary>Busca por documento (RN-01, unicidad; también identifica al cliente en CU-10/CU-11/CU-13).</summary>
    Task<Cliente?> GetByDocumentoAsync(string documento);

    /// <summary>Busca por email (login CU-02, recuperación CU-25, unicidad RN-01).</summary>
    Task<Cliente?> GetByEmailAsync(string email);
}
