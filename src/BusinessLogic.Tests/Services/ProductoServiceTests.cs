using System.Globalization;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Test de ProductoService (CU-18). El repositorio es mock: nunca se toca la base.</summary>
public class ProductoServiceTests
{
    private readonly Mock<IProductoRepository> _productoRepo = new();
    private readonly ProductoService _sut;

    /// <summary> Arma el service bajo prueba con el repositorio mockeado </summary>
    public ProductoServiceTests()
    {
        _sut = new ProductoService(_productoRepo.Object);
    }

    /// <summary> CU-18, alta: datos validos, crean el producto activo </summary>
    [Fact]
    public async Task CrearProductoAsync_WithValidData_CreatesAndReturnsProducto()
    {
        _productoRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Producto?)null);
        _productoRepo.Setup(r => r.CreateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);

        var resultado = await _sut.CrearProductoAsync(new ProductoCreateDTO { Nombre = "Producto", Precio = 200 });

        Assert.Equal("Producto", resultado.Nombre);
        Assert.Equal(200, resultado.Precio);
        Assert.True(resultado.Activo);
    }

    /// <summary> CU-18, flujo alternativo 3a: precio <= 0 lanza excepcion </summary>
    [Fact]
    public async Task CrearProductoAsync_WithNonPositivePrecio_ThrowsPrecioInvalidoException()
    {
        await Assert.ThrowsAsync<PrecioInvalidoException>(() =>
            _sut.CrearProductoAsync(new ProductoCreateDTO { Nombre = "Producto", Precio = -10 }));

        _productoRepo.Verify(r => r.GetByNombreAsync(It.IsAny<string>()), Times.Never);
        _productoRepo.Verify(r => r.CreateAsync(It.IsAny<Producto>()), Times.Never);
    }

    /// <summary> CU-18, flujo alternativo 3b: nombre duplicado lanza excepcion </summary>
    [Fact]
    public async Task CrearProductoAsync_WhenNombreExists_ThrowsProductoDuplicadoException()
    {
        _productoRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync(new Producto());

        await Assert.ThrowsAsync<ProductoDuplicadoException>(() =>
            _sut.CrearProductoAsync(new ProductoCreateDTO { Nombre = "Producto", Precio = 100 }));

        _productoRepo.Verify(r => r.CreateAsync(It.IsAny<Producto>()), Times.Never);
    }

    /// <summary> CU-18, modificar: actualiza solo campos informados </summary>
    [Fact]
    public async Task ActualizarProductoAsync_WithValidData_UpdatesAndReturnsProducto()
    {
        var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Producto", Precio = 100, Activo = true };
        _productoRepo.Setup(r => r.GetByIdAsync(producto.Id)).ReturnsAsync(producto);

        var resultado = await _sut.ActualizarProductoAsync(producto.Id, new ProductoUpdateDTO { Precio = 200 });

        Assert.Equal(200, resultado.Precio);
        Assert.Equal("Producto", resultado.Nombre);
        Assert.True(resultado.Activo);
    }

    /// <summary> CU-18, flujo alternativo 3c: modificar Id inexistente lanza excepcion </summary>
    [Fact]
    public async Task ActualizarProductoAsync_WithNonExistentId_ThrowsProductoNotFoundException()
    {
        _productoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Producto?)null);

        await Assert.ThrowsAsync<ProductoNotFoundException>(() =>
            _sut.ActualizarProductoAsync(Guid.NewGuid(), new ProductoUpdateDTO { Precio = 200 }));
    }

    /// <summary> CU-18, Desactivar: baja logica </summary>
    [Fact]
    public async Task DesactivarProductoAsync_WithExistingId_DeactivatesProducto()
    {
        var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Producto", Precio = 100, Activo = true };
        _productoRepo.Setup(r => r.GetByIdAsync(producto.Id)).ReturnsAsync(producto);

        var resultado = await _sut.DesactivarProductoAsync(producto.Id);

        Assert.False(resultado.Activo);
        _productoRepo.Verify(r => r.UpdateAsync(producto), Times.Once);
    }

    /// <summary> CU-18, Nombre de otro producto en la modificacion, lanza excepcion </summary>
    [Fact]
    public async Task ActualizarProductoAsync_WhenNombreExists_ThrowsProductoDuplicadoException()
    {
        var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Producto", Precio = 100, Activo = true };
        _productoRepo.Setup(r => r.GetByIdAsync(producto.Id)).ReturnsAsync(producto);
        var otro = new Producto { Id = Guid.NewGuid(), Nombre = "Otro Producto", Precio = 50, Activo = true };
        _productoRepo.Setup(r => r.GetByNombreAsync(otro.Nombre)).ReturnsAsync(otro);

        await Assert.ThrowsAsync<ProductoDuplicadoException>(() =>
            _sut.ActualizarProductoAsync(producto.Id, new ProductoUpdateDTO { Nombre = otro.Nombre }));

        _productoRepo.Verify(r => r.UpdateAsync(producto), Times.Never);
    }

    /// <summary>
    /// CU-18, flujo alternativo 3a: precio con más de 6 dígitos enteros o más de 2 decimales
    /// lanza excepción (RN-25). El precio va como string porque C# no admite decimal en atributos.
    /// </summary>
    [Theory]
    [InlineData("1000000")]
    [InlineData("1.999")]
    [InlineData("0.99999999")]
    public async Task CrearProductoAsync_WithPrecioOutOfFormat_ThrowsPrecioInvalidoException(string precio)
    {
        var dto = new ProductoCreateDTO { Nombre = "Producto", Precio = decimal.Parse(precio, CultureInfo.InvariantCulture) };

        await Assert.ThrowsAsync<PrecioInvalidoException>(() => _sut.CrearProductoAsync(dto));

        _productoRepo.Verify(r => r.CreateAsync(It.IsAny<Producto>()), Times.Never);
    }

    /// <summary> CU-18, alta: el precio máximo permitido (999999.99) se acepta (caso borde de RN-25). </summary>
    [Fact]
    public async Task CrearProductoAsync_WithMaximumPrecio_CreatesProducto()
    {
        _productoRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Producto?)null);
        _productoRepo.Setup(r => r.CreateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);

        var resultado = await _sut.CrearProductoAsync(new ProductoCreateDTO { Nombre = "Producto", Precio = 999_999.99m });

        Assert.Equal(999_999.99m, resultado.Precio);
    }

    /// <summary> CU-18, modificación: un precio con más de 2 decimales lanza excepción y no se guarda (RN-25). </summary>
    [Fact]
    public async Task ActualizarProductoAsync_WithTooManyDecimals_ThrowsPrecioInvalidoException()
    {
        var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Producto", Precio = 100, Activo = true };
        _productoRepo.Setup(r => r.GetByIdAsync(producto.Id)).ReturnsAsync(producto);

        await Assert.ThrowsAsync<PrecioInvalidoException>(() =>
            _sut.ActualizarProductoAsync(producto.Id, new ProductoUpdateDTO { Precio = 10.555m }));

        _productoRepo.Verify(r => r.UpdateAsync(producto), Times.Never);
    }

    /// <summary> CU-18, consulta del admin: devuelve activos e inactivos, ordenados por nombre. </summary>
    [Fact]
    public async Task ListarProductosAsync_ReturnsAllProductosOrderedByNombre()
    {
        _productoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Producto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Pizza", Precio = 100, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Empanada", Precio = 50, Activo = false }
        });

        var resultado = await _sut.ListarProductosAsync();

        Assert.Equal(["Empanada", "Pizza"], resultado.Items.Select(p => p.Nombre));
        Assert.Null(resultado.Mensaje);
    }

    /// <summary> CU-18, consulta del empleado: los productos desactivados no aparecen (RN-26). </summary>
    [Fact]
    public async Task ListarProductosActivosAsync_ExcludesInactiveProductos()
    {
        _productoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Producto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Pizza", Precio = 100, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Empanada", Precio = 50, Activo = false }
        });

        var resultado = await _sut.ListarProductosActivosAsync();

        Assert.Equal("Pizza", Assert.Single(resultado.Items).Nombre);
    }

    /// <summary> CU-18, consulta sin productos: lista vacía con mensaje, no es un error. </summary>
    [Fact]
    public async Task ListarProductosActivosAsync_WhenNoProductos_ReturnsEmptyListWithMensaje()
    {
        _productoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Producto>());

        var resultado = await _sut.ListarProductosActivosAsync();

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay productos disponibles", resultado.Mensaje);
    }
}
