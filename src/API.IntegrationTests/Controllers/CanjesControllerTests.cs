using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FidelixAPI.API.IntegrationTests.Fakes;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración de `CanjesController` (CU-07) a través del pipeline HTTP real,
/// incluido el token JWT del login (CU-02). El cliente demo arranca con 150 puntos disponibles.
/// </summary>
public class CanjesControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/canjes";
    private const string EmailDemo = "cliente.demo@fidelix.local";
    private const string PasswordDemo = "Cliente123!";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public CanjesControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Hace login (CU-02) y deja el token en el header `Authorization: Bearer` del cliente HTTP.</summary>
    private static async Task AutenticarAsync(HttpClient client, string email, string password)
    {
        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = password });
        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponseDTO>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    /// <summary>Da de alta (como admin) un beneficio con nombre único (CU-17) y devuelve su Id.</summary>
    private async Task<Guid> CrearBeneficioAsync(int costo)
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO
        {
            Nombre = $"Beneficio {Guid.NewGuid():N}",
            CostoPuntos = costo
        });
        return (await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>())!.Id;
    }

    /// <summary>Registra un cliente nuevo sin puntos (CU-01), con documento y email únicos, y lo deja autenticado.</summary>
    private static async Task<string> RegistrarYAutenticarClienteNuevoAsync(HttpClient client)
    {
        var documento = Random.Shared.Next(100_000_000, 999_999_999).ToString();
        var email = $"cliente.{documento}@fidelix.local";
        await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Cliente Nuevo",
            Documento = documento,
            Email = email,
            Password = "Nuevo1234!"
        });
        await AutenticarAsync(client, email, "Nuevo1234!");
        return documento;
    }

    /// <summary>CU-07, flujo principal: el cliente demo (150 pts) canjea un beneficio de 100 → 201 y le quedan 50.</summary>
    [Fact]
    public async Task CrearCanje_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClient();
        var beneficioId = await CrearBeneficioAsync(costo: 100);
        await AutenticarAsync(client, EmailDemo, PasswordDemo);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = beneficioId });
        var canje = await respuesta.Content.ReadFromJsonAsync<CanjeResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(50, canje!.SaldoRestante);
        Assert.Equal("Pendiente", canje.Estado);
    }

    /// <summary>CU-07, precondición: sin token no se puede canjear (401 Unauthorized).</summary>
    [Fact]
    public async Task CrearCanje_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>CU-07, flujo 2a: un JSON sin `beneficioId` devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task CrearCanje_WithMissingBeneficioId_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        await AutenticarAsync(client, EmailDemo, PasswordDemo);

        var respuesta = await client.PostAsJsonAsync(Url, new { });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-07, flujo 3a: un beneficio inexistente devuelve 404 Not Found.</summary>
    [Fact]
    public async Task CrearCanje_WithNonExistentBeneficio_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        await AutenticarAsync(client, EmailDemo, PasswordDemo);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-07, flujo 3b: un beneficio desactivado devuelve 409 Conflict (RN-17).</summary>
    [Fact]
    public async Task CrearCanje_WithInactiveBeneficio_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var beneficioId = await CrearBeneficioAsync(costo: 10);
        await _factory.CreateClientComo(Roles.Admin).PatchAsync($"/api/admin/beneficios/{beneficioId}/desactivar", null);
        await AutenticarAsync(client, EmailDemo, PasswordDemo);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-07, flujo 3c: un cliente nuevo, sin puntos, no puede canjear (409 Conflict, RN-08).</summary>
    [Fact]
    public async Task CrearCanje_WithInsufficientBalance_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var beneficioId = await CrearBeneficioAsync(costo: 100);
        await RegistrarYAutenticarClienteNuevoAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>
    /// CU-07 + CU-10, N2: los puntos recién acumulados quedan pendientes 15 días, así que
    /// todavía no alcanzan para canjear aunque sean muchos (409 Conflict).
    /// </summary>
    [Fact]
    public async Task CrearCanje_WithOnlyPendingPoints_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var beneficioId = await CrearBeneficioAsync(costo: 100);
        var documento = await RegistrarYAutenticarClienteNuevoAsync(client);

        // Compra de $10000 → 1000 puntos, pero pendientes (N2).
        var producto = await (await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO
        {
            Nombre = $"Producto {Guid.NewGuid():N}",
            Precio = 10000
        })).Content.ReadFromJsonAsync<ProductoResponseDTO>();
        await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/movimientos/acumulaciones", new AcumulacionCreateDTO
        {
            ClienteDocumento = documento,
            Items = [new ItemCompraDTO { ProductoId = producto!.Id, Cantidad = 1 }]
        });

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-07, flujo 5a: si falla la persistencia de movimientos, la API responde 500.</summary>
    [Fact]
    public async Task CrearCanje_WhenPersistenceFails_Returns500InternalServerError()
    {
        var beneficioId = await CrearBeneficioAsync(costo: 10);

        // Reemplaza el repositorio real por uno que siempre falla, solo para este test.
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IMovimientoRepository, MovimientoRepositoryQueFalla>()))
            .CreateClient();
        await AutenticarAsync(client, EmailDemo, PasswordDemo);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjeCreateDTO { BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
    }
}
