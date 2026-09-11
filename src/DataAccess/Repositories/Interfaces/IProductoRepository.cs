using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Producto"/> (CU-18).</summary>
public interface IProductoRepository : IRepositorioBase<Producto>
{
    /// <summary>Busca por nombre (RN-24, unicidad).</summary>
    Task<Producto?> GetByNombreAsync(string nombre);
}
