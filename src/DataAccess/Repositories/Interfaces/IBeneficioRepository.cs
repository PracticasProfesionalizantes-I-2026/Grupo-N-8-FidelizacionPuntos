using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Beneficio"/> (CU-06, CU-07, CU-11, CU-17).</summary>
public interface IBeneficioRepository : IRepositorioBase<Beneficio>
{
    /// <summary>Busca por nombre (RN-23, unicidad).</summary>
    Task<Beneficio?> GetByNombreAsync(string nombre);

    /// <summary>Catálogo de beneficios vigentes para el cliente (CU-06, RN-17).</summary>
    Task<IReadOnlyList<Beneficio>> GetActivosAsync();
}
