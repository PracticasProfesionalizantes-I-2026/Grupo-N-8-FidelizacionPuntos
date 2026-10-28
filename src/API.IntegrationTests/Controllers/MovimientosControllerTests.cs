using System.Net;
using System.Net.Http.Json;
using FidelixAPI.API.IntegrationTests.Fakes;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.DTOs.Producto;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de `MovimientosController` (CU-10) a través del pipeline HTTP real.</summary>
public class MovimientosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/movimientos/acumulaciones";
    private const string DocumentoDemo = "40333444";
    private const string DocumentoGold = "40111222";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public MovimientosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Da de alta un producto de $1000 con nombre único (CU-18) y devuelve su Id.</summary>
    private static async Task<Guid> CrearProductoAsync(HttpClient client)
    {
        var respuesta = await client.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO
        {
            Nombre = $"Producto {Guid.NewGuid():N}",
            Precio = 1000
        });
        return (await respuesta.Content.ReadFromJsonAsync<ProductoResponseDTO>())!.Id;
    }

    /// <summary>Compra de 2 unidades de un producto ($2000).</summary>
    private static AcumulacionCreateDTO Compra(string documento, Guid productoId, int cantidad = 2) => new()
    {
        ClienteDocumento = documento,
        Items = [new ItemCompraDTO { ProductoId = productoId, Cantidad = cantidad }]
    };

    /// <summary>CU-10, flujo principal: con la regla sembrada (0.1 pts/$), $2000 acreditan 200 puntos (201 Created).</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClient();
        var productoId = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, Compra(DocumentoDemo, productoId));
        var acumulacion = await respuesta.Content.ReadFromJsonAsync<AcumulacionResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(200, acumulacion!.PuntosAcreditados);
    }

    /// <summary>CU-10, N1: el cliente Gold sembrado acumula x1.5 ($2000 → 300 puntos).</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WithGoldCliente_AppliesMultiplier()
    {
        var client = _factory.CreateClient();
        var productoId = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, Compra(DocumentoGold, productoId));
        var acumulacion = await respuesta.Content.ReadFromJsonAsync<AcumulacionResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(300, acumulacion!.PuntosAcreditados);
    }

    /// <summary>CU-10, flujo 2a: una cantidad 0 devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WithInvalidItem_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var productoId = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, Compra(DocumentoDemo, productoId, cantidad: 0));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-10, flujo 3a: un documento no registrado devuelve 404 Not Found.</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WithUnknownDocumento_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        var productoId = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, Compra("99999999", productoId));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-10, flujo 3b: un documento con letras devuelve 400 Bad Request.</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WithMalformedDocumento_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var productoId = await CrearProductoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, Compra("abc123", productoId));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-10, flujo 4a: si falla la persistencia de movimientos, la API responde 500.</summary>
    [Fact]
    public async Task RegistrarAcumulacion_WhenPersistenceFails_Returns500InternalServerError()
    {
        var clienteNormal = _factory.CreateClient();
        var productoId = await CrearProductoAsync(clienteNormal);

        // Reemplaza el repositorio real por uno que siempre falla, solo para este test.
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IMovimientoRepository, MovimientoRepositoryQueFalla>()))
            .CreateClient();

        var respuesta = await client.PostAsJsonAsync(Url, Compra(DocumentoDemo, productoId));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
    }
}
