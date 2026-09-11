using FidelixAPI.Shared.DTOs.Producto;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de productos por el admin (CU-18).</summary>
public interface IProductoService
{
    Task<ProductoResponseDTO> CrearAsync(ProductoCreateDTO dto);
    Task<ProductoResponseDTO> ActualizarAsync(Guid id, ProductoUpdateDTO dto);

    /// <summary>Desactivación lógica (RN-26): el producto deja de estar disponible.</summary>
    Task<ProductoResponseDTO> DesactivarAsync(Guid id);
}
