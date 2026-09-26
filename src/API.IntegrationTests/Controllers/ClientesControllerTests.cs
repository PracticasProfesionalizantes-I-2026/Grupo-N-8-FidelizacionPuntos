using System.Net;
using System.Net.Http.Json;
using System.Text;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.DataAccess.Seed;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración de `ClientesController` a través del pipeline HTTP
/// real: autorregistro (CU-01) y consulta de saldo (CU-04).
/// </summary>
public class ClientesControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public ClientesControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>CU-01, flujo principal: datos válidos devuelven 201 Created.</summary>
    [Fact]
    public async Task RegistrarCliente_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Nuevo Cliente",
            Documento = "41000001",
            Email = "nuevo.cliente@test.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    /// <summary>CU-01, flujo 1a: un JSON mal formado devuelve 400 Bad Request (model binding).</summary>
    [Fact]
    public async Task RegistrarCliente_WithInvalidJson_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var contenido = new StringContent("{ esto no es json", Encoding.UTF8, "application/json");

        var respuesta = await client.PostAsync("/api/clientes/registro", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-01, flujo 2a: un campo obligatorio vacío devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task RegistrarCliente_WithMissingRequiredField_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Sin Email",
            Documento = "41000002",
            Email = "",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-01, flujo 3a: el documento del cliente sembrado devuelve 409 Conflict.</summary>
    [Fact]
    public async Task RegistrarCliente_WhenDuplicateDocumentoOrEmail_Returns409Conflict()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Duplicado",
            Documento = "40333444", // Documento del cliente demo del DbInitializer.
            Email = "otro.email@test.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-01, flujo 3b: una contraseña corta devuelve 400 Bad Request.</summary>
    [Fact]
    public async Task RegistrarCliente_WithWeakPassword_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Clave Debil",
            Documento = "41000003",
            Email = "clave.debil@test.com",
            Password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-04, flujo principal: el cliente demo tiene 150 puntos vigentes (el lote vencido de 80 no suma).</summary>
    [Fact]
    public async Task ConsultarSaldo_WithExistingCliente_Returns200OK()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.GetAsync($"/api/clientes/{DbInitializer.ClienteDemoId}/saldo");
        var saldo = await respuesta.Content.ReadFromJsonAsync<SaldoResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(150, saldo!.PuntosDisponibles);
    }

    /// <summary>CU-04 (temporal, mientras el Id viaja en la ruta): un cliente inexistente devuelve 404 Not Found.</summary>
    [Fact]
    public async Task ConsultarSaldo_WithNonExistentCliente_Returns404NotFound()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.GetAsync($"/api/clientes/{Guid.NewGuid()}/saldo");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-04, flujo 4a: si falla la lectura de movimientos, la API responde 500.</summary>
    [Fact]
    public async Task ConsultarSaldo_WhenPersistenceFails_Returns500InternalServerError()
    {
        // Reemplaza el repositorio real por uno que siempre falla, solo para este test.
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IMovimientoRepository, MovimientoRepositoryQueFalla>()))
            .CreateClient();

        var respuesta = await client.GetAsync($"/api/clientes/{DbInitializer.ClienteDemoId}/saldo");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
    }

    /// <summary>Repositorio falso que simula una caída de la base en cualquier operación.</summary>
    private class MovimientoRepositoryQueFalla : IMovimientoRepository
    {
        /// <summary>Simula una falla de conexión.</summary>
        public Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
            throw new InvalidOperationException("Falla de conexión simulada");

        /// <summary>Simula una falla de conexión.</summary>
        public Task<Movimiento?> GetByIdAsync(Guid id) => throw new InvalidOperationException("Falla de conexión simulada");

        /// <summary>Simula una falla de conexión.</summary>
        public Task<IReadOnlyList<Movimiento>> GetAllAsync() => throw new InvalidOperationException("Falla de conexión simulada");

        /// <summary>Simula una falla de conexión.</summary>
        public Task<Movimiento> CreateAsync(Movimiento entity) => throw new InvalidOperationException("Falla de conexión simulada");

        /// <summary>Simula una falla de conexión.</summary>
        public Task UpdateAsync(Movimiento entity) => throw new InvalidOperationException("Falla de conexión simulada");
    }
}
