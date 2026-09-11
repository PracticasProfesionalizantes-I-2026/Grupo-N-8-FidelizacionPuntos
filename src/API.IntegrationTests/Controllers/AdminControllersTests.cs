using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Producto;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de los controllers admin: ABM (CU-18) y autorización por rol.</summary>
public class AdminControllersTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    public AdminControllersTests(FidelixApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.InicializarBaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CrearProducto_AsAdmin_Returns201Created()
    {
        var admin = await _factory.ClienteAutenticadoAsync("admin@fidelix.local", "Admin123!");

        var respuesta = await admin.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "Producto Nuevo", Precio = 500m });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var producto = await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>();
        Assert.True(producto!.Activo);
    }

    [Fact]
    public async Task CrearProducto_WithDuplicateName_Returns409Conflict()
    {
        var admin = await _factory.ClienteAutenticadoAsync("admin@fidelix.local", "Admin123!");

        var respuesta = await admin.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "Producto Demo A", Precio = 500m });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearProducto_AsCliente_Returns403Forbidden()
    {
        var cliente = await _factory.ClienteAutenticadoAsync("cliente.demo@fidelix.local", "Cliente123!");

        var respuesta = await cliente.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "Otro", Precio = 500m });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearProducto_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "Otro", Precio = 500m });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task ObtenerReglasAcumulacion_AsAdmin_Returns200OK()
    {
        var admin = await _factory.ClienteAutenticadoAsync("admin@fidelix.local", "Admin123!");

        var respuesta = await admin.GetAsync("/api/admin/reglas-acumulacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
