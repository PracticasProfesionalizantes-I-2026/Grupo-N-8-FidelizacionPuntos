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

/// <summary>Tests de integración del canje presencial por el empleado (`CanjesController.CrearCanjePresencial`, CU-11).</summary>
public class CanjesPresencialControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/canjes/presencial";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public CanjesPresencialControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Da de alta (como admin) un beneficio con nombre único y devuelve su Id.</summary>
    private async Task<Guid> CrearBeneficioAsync(int costo)
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync("/api/admin/beneficios",
            new BeneficioCreateDTO { Nombre = $"Beneficio {Guid.NewGuid():N}", CostoPuntos = costo });
        return (await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>())!.Id;
    }

    /// <summary>Crea (como empleado) un cliente nuevo y le carga puntos disponibles; devuelve su documento.</summary>
    private async Task<string> ClienteConPuntosAsync(int puntos)
    {
        var documento = TestHelpers.DocumentoUnico();
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/clientes",
            new ClienteCreateDTO { Nombre = "Cliente Local", Documento = documento, Email = $"local.{documento}@fidelix.local" });
        var cliente = await respuesta.Content.ReadFromJsonAsync<ClienteResponseDTO>();
        if (puntos > 0) await _factory.AcreditarPuntosAsync(cliente!.Id, puntos);
        return documento;
    }

    /// <summary>CU-11: el empleado canjea por el cliente: 201 y el canje queda "Entregado".</summary>
    [Fact]
    public async Task CrearCanjePresencial_WithValidData_Returns201Created()
    {
        var documento = await ClienteConPuntosAsync(300);
        var beneficioId = await CrearBeneficioAsync(100);

        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = documento, BeneficioId = beneficioId });
        var canje = await respuesta.Content.ReadFromJsonAsync<CanjeResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("Entregado", canje!.Estado);
        Assert.Equal(200, canje.SaldoRestante);
    }

    /// <summary>CU-11, flujo 1a: un documento que no existe devuelve 404.</summary>
    [Fact]
    public async Task CrearCanjePresencial_WithUnknownDocumento_Returns404NotFound()
    {
        var beneficioId = await CrearBeneficioAsync(100);

        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = "99999999", BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-11, flujo 3a: un beneficio desactivado devuelve 409 (RN-17).</summary>
    [Fact]
    public async Task CrearCanjePresencial_WithInactiveBeneficio_Returns409Conflict()
    {
        var documento = await ClienteConPuntosAsync(300);
        var beneficioId = await CrearBeneficioAsync(100);
        await _factory.CreateClientComo(Roles.Admin).PatchAsync($"/api/admin/beneficios/{beneficioId}/desactivar", null);

        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = documento, BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-11, flujo 3b: un cliente sin puntos no puede canjear (409, RN-08).</summary>
    [Fact]
    public async Task CrearCanjePresencial_WithInsufficientBalance_Returns409Conflict()
    {
        var documento = await ClienteConPuntosAsync(0);
        var beneficioId = await CrearBeneficioAsync(100);

        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = documento, BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-11, flujo 5a: si falla la persistencia de movimientos, la API responde 500.</summary>
    [Fact]
    public async Task CrearCanjePresencial_WhenPersistenceFails_Returns500InternalServerError()
    {
        var documento = await ClienteConPuntosAsync(300);
        var beneficioId = await CrearBeneficioAsync(100);
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddScoped<IMovimientoRepository, MovimientoRepositoryQueFalla>())).CreateClient();
        _factory.Autenticar(client, Roles.Empleado);

        var respuesta = await client.PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = documento, BeneficioId = beneficioId });

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
    }

    /// <summary>RN-11: un cliente no puede usar el canje presencial (403).</summary>
    [Fact]
    public async Task CrearCanjePresencial_WithClienteToken_Returns403Forbidden()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Cliente).PostAsJsonAsync(Url, new CanjePresencialCreateDTO { ClienteDocumento = "40333444", BeneficioId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>RN-10: el cliente canjea desde la app (Pendiente), el empleado lo entrega y no se puede entregar dos veces.</summary>
    [Fact]
    public async Task MarcarEntregado_WithPendingCanje_Returns200ThenConflict()
    {
        var cliente = _factory.CreateClient();
        var (id, _, _) = await TestHelpers.RegistrarYAutenticarClienteAsync(cliente);
        await _factory.AcreditarPuntosAsync(id, 300);
        var beneficioId = await CrearBeneficioAsync(100);
        var canje = await (await cliente.PostAsJsonAsync("/api/canjes", new CanjeCreateDTO { BeneficioId = beneficioId }))
            .Content.ReadFromJsonAsync<CanjeResponseDTO>();
        var empleado = _factory.CreateClientComo(Roles.Empleado);

        var primera = await empleado.PatchAsync($"/api/canjes/{canje!.Id}/entregar", null);
        var segunda = await empleado.PatchAsync($"/api/canjes/{canje.Id}/entregar", null);
        var entregado = await primera.Content.ReadFromJsonAsync<CanjeItemDTO>();

        Assert.Equal("Pendiente", canje.Estado);
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal("Entregado", entregado!.Estado);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    /// <summary>RN-10: entregar un canje que no existe devuelve 404.</summary>
    [Fact]
    public async Task MarcarEntregado_WithUnknownId_Returns404NotFound()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PatchAsync($"/api/canjes/{Guid.NewGuid()}/entregar", null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
