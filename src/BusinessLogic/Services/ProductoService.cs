using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IProductoService (CU-18). </summary>
public class ProductoService(IProductoRepository productoRepository) : IProductoService
{
    /// <summary> Precio máximo de un producto: hasta 6 dígitos enteros y 2 decimales (RN-25). </summary>
    private const decimal PrecioMaximo = 999_999.99m;
    /// <summary> Cantidad máxima de decimales permitida en un precio (RN-25). </summary>
    private const int DecimalesMaximos = 2;

    /// <summary> Alta: nombre único (RN-24) y precio válido (RN-25). </summary>
    public async Task<ProductoResponseDTO> CrearProductoAsync(ProductoCreateDTO dto)
    {
        ValidarPrecio(dto.Precio);

        if (await productoRepository.GetByNombreAsync(dto.Nombre) is not null)
        {
            throw new ProductoDuplicadoException("El producto ingresado ya existe.");
        }

        var producto = await productoRepository.CreateAsync(new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Activo = true
        });

        return MapToResponseDTO(producto);
    }

    /// <summary> Modificar un producto, actualizacion parcial: nombre único (RN-24) y precio válido (RN-25). </summary>
    public async Task<ProductoResponseDTO> ActualizarProductoAsync(int id, ProductoUpdateDTO dto)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("El producto no existe.");

        if (dto.Precio is not null)
        {
            ValidarPrecio(dto.Precio.Value);
        }

        if (dto.Nombre is not null && dto.Nombre != producto.Nombre)
        {
            var otro = await productoRepository.GetByNombreAsync(dto.Nombre);
            if (otro is not null && otro.Id != id)
            {
                throw new ProductoDuplicadoException("El producto ingresado ya existe.");
            }

            producto.Nombre = dto.Nombre;
        }

        if (dto.Descripcion is not null) producto.Descripcion = dto.Descripcion;
        if (dto.Precio is not null) producto.Precio = dto.Precio.Value;

        await productoRepository.UpdateAsync(producto);
        return MapToResponseDTO(producto);
    }

    /// <summary> Desactivar un producto (RN-26). </summary>
    public async Task<ProductoResponseDTO> DesactivarProductoAsync(int id)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("El producto no existe.");

        producto.Activo = false;
        await productoRepository.UpdateAsync(producto);
        return MapToResponseDTO(producto);
    }

    /// <summary> Consulta del admin: todos los productos ordenados por nombre, incluidos los desactivados. </summary>
    public async Task<ListadoResponseDTO<ProductoResponseDTO>> ListarProductosAsync()
    {
        var productos = await productoRepository.GetAllAsync();
        var items = productos.OrderBy(p => p.Nombre).Select(MapToResponseDTO).ToList();
        return ListadoResponseDTO<ProductoResponseDTO>.Crear(items, "No hay productos registrados");
    }

    /// <summary> Consulta del empleado: solo los productos activos (RN-26), ordenados por nombre. </summary>
    public async Task<ListadoResponseDTO<ProductoResponseDTO>> ListarProductosActivosAsync()
    {
        var productos = await productoRepository.GetAllAsync();
        var items = productos.Where(p => p.Activo).OrderBy(p => p.Nombre).Select(MapToResponseDTO).ToList();
        return ListadoResponseDTO<ProductoResponseDTO>.Crear(items, "No hay productos disponibles");
    }

    /// <summary>
    /// RN-25: el precio debe ser mayor a 0, no superar 999.999,99 (6 dígitos enteros)
    /// y tener como máximo 2 decimales. Lo comparten el alta y la modificación.
    /// </summary>
    private static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0)
        {
            throw new PrecioInvalidoException("El precio debe ser mayor a 0");
        }

        if (precio > PrecioMaximo)
        {
            throw new PrecioInvalidoException("El precio no puede superar 999999.99");
        }

        // Si redondear a 2 decimales cambia el valor, es que tenía más de 2 (ej. 0.999 → 1.00).
        if (decimal.Round(precio, DecimalesMaximos) != precio)
        {
            throw new PrecioInvalidoException("El precio puede tener como máximo 2 decimales");
        }
    }

    /// <summary>Traduce la entidad al DTO público.</summary>
    private static ProductoResponseDTO MapToResponseDTO(Producto p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Precio = p.Precio,
        Activo = p.Activo
    };
}
