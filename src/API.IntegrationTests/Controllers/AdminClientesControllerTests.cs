using System.Net;
using System.Net.Http.Json;
using FidelixAPI.API.IntegrationTests.Fakes;
using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Repositories;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.DTOs.Procesos;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración del ABM de clientes por el admin (`AdminClientesController`, CU-15).</summary>
public class AdminClientesControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/admin/clientes";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminClientesControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Crea (como admin) un cliente nuevo con documento único y devuelve su DTO.</summary>
    private async Task<ClienteResponseDTO> CrearClienteAsync()
    {
        var documento = TestHelpers.DocumentoUnico();
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url,
            new ClienteCreateDTO { Nombre = "Cliente Admin", Documento = documento, Email = $"adm.{documento}@fidelix.local" });
        return (await respuesta.Content.ReadFromJsonAsync<ClienteResponseDTO>())!;
    }

    /// <summary>CU-15, alta: datos válidos devuelven 201.</summary>
    [Fact]
    public async Task AbmCliente_Create_WithValidData_Returns201Created()
    {
        var documento = TestHelpers.DocumentoUnico();

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url,
            new ClienteCreateDTO { Nombre = "Nuevo", Documento = documento, Email = $"nuevo.{documento}@fidelix.local" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    /// <summary>CU-15, modificación: el admin pasa al cliente a Gold (N1) y devuelve 200.</summary>
    [Fact]
    public async Task AbmCliente_Update_WithValidData_Returns200OK()
    {
        var cliente = await CrearClienteAsync();

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PutAsJsonAsync($"{Url}/{cliente.Id}", new AdminClienteUpdateDTO { Categoria = CategoriaCliente.Gold });
        var actualizado = await respuesta.Content.ReadFromJsonAsync<ClienteResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(CategoriaCliente.Gold, actualizado!.Categoria);
    }

    /// <summary>CU-15, baja: devuelve 204 y el cliente queda inactivo (RN-14).</summary>
    [Fact]
    public async Task AbmCliente_Delete_WithExistingId_Returns204NoContent()
    {
        var cliente = await CrearClienteAsync();
        var admin = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await admin.DeleteAsync($"{Url}/{cliente.Id}");
        var clientes = await admin.GetFromJsonAsync<List<ClienteResponseDTO>>(Url);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.False(clientes!.Single(c => c.Id == cliente.Id).Activo);
    }

    /// <summary>CU-15, flujo 2a: sin nombre devuelve 400.</summary>
    [Fact]
    public async Task AbmCliente_WithInvalidData_Returns400BadRequest()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, new ClienteCreateDTO { Nombre = "", Documento = "41000111", Email = "x@fidelix.local" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-15, flujo 3a: el documento del cliente demo ya existe (409, RN-01).</summary>
    [Fact]
    public async Task AbmCliente_Create_WhenDocumentoExists_Returns409Conflict()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, new ClienteCreateDTO { Nombre = "Repetido", Documento = "40333444", Email = "otro.demo@fidelix.local" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-15, flujo 3b: modificar un id inexistente devuelve 404.</summary>
    [Fact]
    public async Task AbmCliente_Update_WithNonExistentId_Returns404NotFound()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", new AdminClienteUpdateDTO { Nombre = "X" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
