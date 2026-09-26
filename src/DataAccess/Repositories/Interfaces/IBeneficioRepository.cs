using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Beneficio"/> (CU-17).</summary>
public interface IBeneficioRepository : IRepositorioBase<Beneficio>
{
    /// <summary>Busca por nombre (RN-23, unicidad).</summary>
    Task<Beneficio?> GetByNombreAsync(string nombre);
}
