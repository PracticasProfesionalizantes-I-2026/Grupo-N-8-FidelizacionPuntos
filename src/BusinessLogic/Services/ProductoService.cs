using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IProductoService"/> (CU-18).</summary>
public class ProductoService(IProductoRepository productoRepository, IAuditoriaService auditoriaService) : IProductoService
{
    /// <summary>Alta: nombre único (RN-24) y precio positivo (RN-25).</summary>
    public async Task<ProductoResponseDTO> CrearAsync(ProductoCreateDTO dto)
    {
        if (dto.Precio <= 0)
        {
            throw new PrecioInvalidoException("El precio debe ser mayor a cero.");
        }

        if (await productoRepository.GetByNombreAsync(dto.Nombre) is not null)
        {
            throw new ProductoDuplicadoException("Ya existe un producto con ese nombre.");
        }

        var producto = await productoRepository.CreateAsync(new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("CrearProducto", ActorTipo.Admin, null, "Producto", producto.Id);
        return MapToResponseDTO(producto);
    }

    /// <summary>Modifica solo los campos presentes; valida precio positivo (RN-25) y nombre único si cambió (RN-24).</summary>
    public async Task<ProductoResponseDTO> ActualizarAsync(Guid id, ProductoUpdateDTO dto)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("No se encontró el producto.");

        if (dto.Precio is <= 0)
        {
            throw new PrecioInvalidoException("El precio debe ser mayor a cero.");
        }

        if (dto.Nombre is not null && dto.Nombre != producto.Nombre)
        {
            var otro = await productoRepository.GetByNombreAsync(dto.Nombre);
            if (otro is not null && otro.Id != id)
            {
                throw new ProductoDuplicadoException("Ya existe un producto con ese nombre.");
            }

            producto.Nombre = dto.Nombre;
        }

        if (dto.Descripcion is not null) producto.Descripcion = dto.Descripcion;
        if (dto.Precio is not null) producto.Precio = dto.Precio.Value;

        await productoRepository.UpdateAsync(producto);
        await auditoriaService.RegistrarAsync("ActualizarProducto", ActorTipo.Admin, null, "Producto", id);
        return MapToResponseDTO(producto);
    }

    /// <summary>Desactivación lógica (RN-26): a partir de ahora, el producto no está disponible.</summary>
    public async Task<ProductoResponseDTO> DesactivarAsync(Guid id)
    {
        var producto = await productoRepository.GetByIdAsync(id)
            ?? throw new ProductoNotFoundException("No se encontró el producto.");

        producto.Activo = false;
        await productoRepository.UpdateAsync(producto);
        await auditoriaService.RegistrarAsync("DesactivarProducto", ActorTipo.Admin, null, "Producto", id);
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
