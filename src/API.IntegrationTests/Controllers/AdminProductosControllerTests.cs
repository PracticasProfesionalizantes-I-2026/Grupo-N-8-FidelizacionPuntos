using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Producto;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de `AdminProductosController` (CU-18) a través del pipeline HTTP real.</summary>
public class AdminProductosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminProductosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Crea un producto con nombre único y devuelve su DTO. Helper para los tests que necesitan
    /// un producto existente (modificación, baja, duplicado). El nombre lleva un Guid para que
    /// los tests no choquen entre sí, porque comparten la misma base.
    /// </summary>
    private async Task<ProductoResponseDTO> CrearProductoAsync(HttpClient client)
    {
        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO
        {
            Nombre = $"Producto {Guid.NewGuid():N}",
            Precio = 500
        });
        return (await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>())!;
    }

    /// <summary>CU-18, alta: datos válidos devuelven 201 Created con el producto activo.</summary>
    [Fact]
    public async Task AbmProducto_Create_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO
        {
            Nombre = "Hamburguesa Completa",
            Descripcion = "Con papas fritas",
            Precio = 8500
        });
        var creado = await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.True(creado!.Activo);
    }

    /// <summary>CU-18, flujo 2a: un nombre vacío devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task AbmProducto_WithInvalidData_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "", Precio = 100 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-18, flujo 3a: precio cero devuelve 400 Bad Request (RN-25).</summary>
    [Fact]
    public async Task AbmProducto_Create_WithNonPositivePrecio_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = "Producto Precio Cero", Precio = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-18, flujo 3b: dar de alta un nombre que ya existe devuelve 409 Conflict (RN-24).</summary>
    [Fact]
    public async Task AbmProducto_Create_WhenNombreExists_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var existente = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = existente.Nombre, Precio = 100 });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-18, modificación parcial: cambia solo el precio y el nombre queda intacto (200 OK).</summary>
    [Fact]
    public async Task AbmProducto_Update_WithValidData_Returns200OK()
    {
        var client = _factory.CreateClient();
        var producto = await CrearProductoAsync(client);

        var respuesta = await client.PutAsJsonAsync($"/api/admin/productos/{producto.Id}", new ProductoUpdateDTO { Precio = 750 });
        var actualizado = await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(750, actualizado!.Precio);
        Assert.Equal(producto.Nombre, actualizado.Nombre);
    }

    /// <summary>CU-18, flujo 3c: modificar un Id inexistente devuelve 404 Not Found.</summary>
    [Fact]
    public async Task AbmProducto_Update_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PutAsJsonAsync($"/api/admin/productos/{Guid.NewGuid()}", new ProductoUpdateDTO { Precio = 750 });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-18, flujo 3b en modificación: ponerle a un producto el nombre de otro devuelve 409 Conflict (RN-24).</summary>
    [Fact]
    public async Task AbmProducto_Update_WhenNombreBelongsToOther_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var producto = await CrearProductoAsync(client);
        var otro = await CrearProductoAsync(client);

        var respuesta = await client.PutAsJsonAsync($"/api/admin/productos/{producto.Id}", new ProductoUpdateDTO { Nombre = otro.Nombre });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-18, flujo 2a en modificación: un nombre de solo espacios devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task AbmProducto_Update_WithBlankNombre_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var producto = await CrearProductoAsync(client);

        var respuesta = await client.PutAsJsonAsync($"/api/admin/productos/{producto.Id}", new ProductoUpdateDTO { Nombre = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-18, desactivación: devuelve 200 OK con el producto inactivo (RN-26).</summary>
    [Fact]
    public async Task AbmProducto_Deactivate_WithExistingId_Returns200OK()
    {
        var client = _factory.CreateClient();
        var producto = await CrearProductoAsync(client);

        var respuesta = await client.PatchAsync($"/api/admin/productos/{producto.Id}/desactivar", null);
        var desactivado = await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False(desactivado!.Activo);
    }
}
