using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Producto;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary> ABM y consulta de productos (CU-18). </summary>
public interface IProductoService
{
    /// <summary> Crear un producto. Requiere precio positivo (RN-25) y nombre único (RN-24). </summary>
    Task<ProductoResponseDTO> CrearProductoAsync(ProductoCreateDTO dto);

    /// <summary> Modificar un producto, es parcial ya que puede recibir solo ciertos campos (RN-24 y RN-25). </summary>
    Task<ProductoResponseDTO> ActualizarProductoAsync(int id, ProductoUpdateDTO dto);

    /// <summary> Desactivación lógica (RN-26). El producto deja de estar disponible para la compra. </summary>
    Task<ProductoResponseDTO> DesactivarProductoAsync(int id);

    /// <summary> Lista todos los productos, activos e inactivos, para que el admin gestione el catálogo. </summary>
    Task<ListadoResponseDTO<ProductoResponseDTO>> ListarProductosAsync();

    /// <summary> Lista solo los productos activos (RN-26): los que el empleado puede cargar en una compra (CU-10). </summary>
    Task<ListadoResponseDTO<ProductoResponseDTO>> ListarProductosActivosAsync();
}
