using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="ProductoService"/> (CU-18).</summary>
public class ProductoServiceTests
{
    private readonly Mock<IProductoRepository> _productoRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly ProductoService _sut;

    public ProductoServiceTests()
    {
        _sut = new ProductoService(_productoRepo.Object, _auditoriaService.Object);
    }

    [Fact]
    public async Task CrearAsync_WithValidData_CreatesAndReturnsProducto()
    {
        _productoRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Producto?)null);
        _productoRepo.Setup(r => r.CreateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);

        var resultado = await _sut.CrearAsync(new ProductoCreateDTO { Nombre = "Gaseosa", Precio = 1500m });

        Assert.Equal("Gaseosa", resultado.Nombre);
        Assert.True(resultado.Activo);
    }

    [Fact]
    public async Task CrearAsync_WithInvalidPrecio_ThrowsPrecioInvalidoException()
    {
        await Assert.ThrowsAsync<PrecioInvalidoException>(() =>
            _sut.CrearAsync(new ProductoCreateDTO { Nombre = "Gaseosa", Precio = 0 }));
    }

    [Fact]
    public async Task CrearAsync_WhenNombreExists_ThrowsProductoDuplicadoException()
    {
        _productoRepo.Setup(r => r.GetByNombreAsync("Gaseosa")).ReturnsAsync(new Producto { Nombre = "Gaseosa" });

        await Assert.ThrowsAsync<ProductoDuplicadoException>(() =>
            _sut.CrearAsync(new ProductoCreateDTO { Nombre = "Gaseosa", Precio = 1500m }));
    }

    [Fact]
    public async Task DesactivarAsync_WithExistingProducto_SetsActivoFalse()
    {
        var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Gaseosa", Activo = true };
        _productoRepo.Setup(r => r.GetByIdAsync(producto.Id)).ReturnsAsync(producto);

        var resultado = await _sut.DesactivarAsync(producto.Id);

        Assert.False(resultado.Activo);
    }

    [Fact]
    public async Task DesactivarAsync_WithUnknownProducto_ThrowsProductoNotFoundException()
    {
        _productoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Producto?)null);

        await Assert.ThrowsAsync<ProductoNotFoundException>(() => _sut.DesactivarAsync(Guid.NewGuid()));
    }
}
