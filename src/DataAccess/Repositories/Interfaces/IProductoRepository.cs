using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary> Acceso a datos de Producto (CU-18). </summary>
public interface IProductoRepository : IRepositorioBase<Producto>
{
    /// <summary> Obtiene un producto por su nombre (RN-24). </summary>
    Task<Producto?> GetByNombreAsync(string nombre);
}