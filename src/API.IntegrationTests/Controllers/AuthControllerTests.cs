using System.Net;
using System.Net.Http.Json;
using FidelixAPI.DataAccess.Seed;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Cliente;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración de 'AuthController' a través del pipeline HTTP
/// real: Login Cliente (CU-02)
/// </summary>
public class AuthControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AuthControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>CU-02. Flujo principal: usuario y contraseña validos devuelve 200 Ok.</summary>
    [Fact]
    public async Task LoginCliente_WithValidCredentials_Returns200OK()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
        {
            Email = "cliente.demo@fidelix.local",
            Password = "Cliente123!"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    /// <summary>CU-02. Flujoe alternativo 2a: campos obligatorios faltantes devuelve 400 Bad Request </summary>
    [Fact]
    public async Task LoginCliente_WithMissingField_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
        {
            Email = "",
            Password = "Cliente123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-02. Flujo alternativo 3a: credenciales incorrectas devuelve 401 Unauthorized </summary>
    [Fact]
    public async Task LoginCliente_WithWrongPassword_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
        {
            Email = "cliente.demo@fidelix.local",
            Password = "PasswordIncorrecto"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>CU-02. Flujo alternativo 3c: cuenta inactiva devuelve un 403 Forbidden</summary>
    [Fact]
    public async Task LoginCliente_WhenAccountInactive_Returns403Forbidden()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
        {
            Email = "cliente.inactivo@fidelix.local",
            Password = "Inactivo123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>CU-02. Flujo alternativo 3b: cuenta bloqueada devuelve un 403 Forbidden </summary>
    [Fact]
    public async Task LoginCliente_WhenAccountLocked_Returns403Forbidden()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Cliente Bloqueado",
            Documento = "123456789",
            Email = "cliente.bloqueado@fidelix.local",
            Password = "Bloqueado123!"
        });

        for (int i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
            {
                Email = "cliente.bloqueado@fidelix.local",
                Password = "PasswordIncorrecto"
            });
        }

        var respuesta = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDTO
        {
            Email = "cliente.bloqueado@fidelix.local",
            Password = "Bloqueado123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
