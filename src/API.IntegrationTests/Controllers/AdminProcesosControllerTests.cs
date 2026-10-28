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

/// <summary>
/// Tests de integración de los procesos automáticos (CU-22 vencimiento, CU-26 bono de cumpleaños),
/// ejecutados con el endpoint manual del admin, que llama al mismo service que los jobs diarios.
/// </summary>
public class AdminProcesosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string UrlVencimiento = "/api/admin/procesos/vencimiento-puntos";
    private const string UrlBono = "/api/admin/procesos/bono-cumpleanos";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminProcesosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Registra un cliente nuevo y devuelve su Id y un cliente HTTP con su token.</summary>
    private async Task<(Guid Id, HttpClient Client)> ClienteNuevoAsync(DateTime? fechaNacimiento = null)
    {
        var client = _factory.CreateClient();
        var (id, _, _) = await TestHelpers.RegistrarYAutenticarClienteAsync(client, fechaNacimiento);
        return (id, client);
    }

    /// <summary>CU-22: un lote vencido queda en 0, el saldo baja y aparece el movimiento de vencimiento.</summary>
    [Fact]
    public async Task PointsExpirationJob_WithExpiredLotes_UpdatesSaldoAndNotifiesCliente()
    {
        var (id, client) = await ClienteNuevoAsync();
        await _factory.AcreditarPuntosAsync(id, 70, fechaVencimiento: DateTime.UtcNow.AddDays(-1), fecha: DateTime.UtcNow.AddDays(-200));
        await _factory.AcreditarPuntosAsync(id, 30);

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsync(UrlVencimiento, null);
        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoProcesoDTO>();
        var historial = await client.GetFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>("/api/clientes/me/movimientos?tipoMovimiento=Vencimiento");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotEqual("ErrorParcial", resultado!.Estado);
        Assert.Equal(-70, Assert.Single(historial!.Items).Puntos);
    }

    /// <summary>CU-22, flujo 2a: si se corre dos veces, la segunda no encuentra nada que vencer.</summary>
    [Fact]
    public async Task PointsExpirationJob_WithNoExpiredLotes_LogsNoChanges()
    {
        var admin = _factory.CreateClientComo(Roles.Admin);
        await admin.PostAsync(UrlVencimiento, null);

        var resultado = await (await admin.PostAsync(UrlVencimiento, null)).Content.ReadFromJsonAsync<ResultadoProcesoDTO>();

        Assert.Equal("SinCambios", resultado!.Estado);
    }

    /// <summary>CU-22, flujo 4a: si falla un cliente, el resto se vence igual y el resultado es "ErrorParcial".</summary>
    [Fact]
    public async Task PointsExpirationJob_WhenPersistenceFailsForOneCliente_LogsPartialError()
    {
        var (idFalla, _) = await ClienteNuevoAsync();
        var (idOk, clientOk) = await ClienteNuevoAsync();
        await _factory.AcreditarPuntosAsync(idFalla, 10, fechaVencimiento: DateTime.UtcNow.AddDays(-1));
        await _factory.AcreditarPuntosAsync(idOk, 20, fechaVencimiento: DateTime.UtcNow.AddDays(-1));
        var admin = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddScoped<IMovimientoRepository>(sp => new MovimientoRepositoryQueFallaParaUnCliente(
                new MovimientoRepository(sp.GetRequiredService<FidelixDbContext>()), idFalla)))).CreateClient();
        _factory.Autenticar(admin, Roles.Admin);

        var resultado = await (await admin.PostAsync(UrlVencimiento, null)).Content.ReadFromJsonAsync<ResultadoProcesoDTO>();
        var historialOk = await clientOk.GetFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>("/api/clientes/me/movimientos?tipoMovimiento=Vencimiento");

        Assert.Equal("ErrorParcial", resultado!.Estado);
        Assert.Equal(1, resultado.ClientesConError);
        Assert.Single(historialOk!.Items);
    }

    /// <summary>CU-26: un cliente que cumple años hoy recibe el bono una sola vez en el año (RN-29).</summary>
    [Fact]
    public async Task BonoCumpleanosJob_WithMatchingCliente_AccruesPointsOncePerYear()
    {
        var (_, client) = await ClienteNuevoAsync(fechaNacimiento: DateTime.UtcNow.Date.AddYears(-30));
        var admin = _factory.CreateClientComo(Roles.Admin);

        await admin.PostAsync(UrlBono, null);
        await admin.PostAsync(UrlBono, null);
        var bonos = await client.GetFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>("/api/clientes/me/movimientos?tipoMovimiento=BonoCumpleanos");
        var saldo = await client.GetFromJsonAsync<SaldoResponseDTO>("/api/clientes/me/saldo");

        Assert.Equal(500, Assert.Single(bonos!.Items).Puntos);
        Assert.Equal(500, saldo!.PuntosDisponibles);
    }
}
