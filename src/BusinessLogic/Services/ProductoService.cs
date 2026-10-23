using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IProductoService (CU-18). </summary>
public class ProductoService(IProductoRepository productoRepository) : IProductoService
{
    /// <summary> Alta: nombre único (RN-24) y precio positivo (RN-25). </summary>
    public async Task<ProductoResponseDTO> CrearProductoAsync(ProductoCreateDTO dto)
    {
        if (dto.Precio <= 0)
        {
            throw new PrecioInvalidoException("El precio debe ser mayor a 0");
        }

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

    /// <summary> Modificar un producto, actualizacion parcial: nombre único (RN-24) y precio positivo (RN-25). </summary>
    public async Task<ProductoResponseDTO> ActualizarProductoAsync(Guid id, ProductoUpdateDTO dto)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("El producto no existe.");

        if (dto.Precio is <= 0)
        {
            throw new PrecioInvalidoException("El precio debe ser mayor a 0");
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
    public async Task<ProductoResponseDTO> DesactivarProductoAsync(Guid id)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("El producto no existe.");

        producto.Activo = false;
        await productoRepository.UpdateAsync(producto);
        return MapToResponseDTO(producto);
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
