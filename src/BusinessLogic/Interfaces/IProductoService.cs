using FidelixAPI.Shared.DTOs.Producto;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary> ABM de productos por el admin (CU-18). </summary>
public interface IProductoService
{
    /// <summary> Crear un producto. Requiere precio positivo (RN-25) y nombre único (RN-24). </summary>
    Task<ProductoResponseDTO> CrearProductoAsync(ProductoCreateDTO dto);

    /// <summary> Modificar un producto, es parcial ya que puede recibir solo ciertos campos (RN-24 y RN-25). </summary>
    Task<ProductoResponseDTO> ActualizarProductoAsync(Guid id, ProductoUpdateDTO dto);

    /// <summary> Desactivación lógica (RN-26). El producto deja de estar disponible para la compra. </summary>
    Task<ProductoResponseDTO> DesactivarProductoAsync(Guid id);
}
