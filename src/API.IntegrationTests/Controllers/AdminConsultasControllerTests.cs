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
/// Tests de integración de las consultas del admin: movimientos (CU-20), auditoría (CU-21) y
/// reportes (CU-24).
/// </summary>
public class AdminConsultasControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminConsultasControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>CU-20: el admin filtra los movimientos del cliente demo y recibe acumulaciones.</summary>
    [Fact]
    public async Task ConsultarMovimientos_WithValidFilters_Returns200OK()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync($"/api/admin/movimientos?clienteId={FidelixAPI.DataAccess.Seed.DbInitializer.ClienteDemoId}");
        var movimientos = await respuesta.Content.ReadFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotEmpty(movimientos!.Items);
    }

    /// <summary>CU-20, flujo 2a: una fecha mal formada devuelve 400.</summary>
    [Fact]
    public async Task ConsultarMovimientos_WithInvalidDateFilter_Returns400BadRequest()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync("/api/admin/movimientos?fechaHasta=31-31-2026");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-20, flujo 4a: un cliente sin movimientos da 200 con lista vacía y mensaje.</summary>
    [Fact]
    public async Task ConsultarMovimientos_WithNoMatches_Returns200OKWithEmptyList()
    {
        var movimientos = await _factory.CreateClientComo(Roles.Admin)
            .GetFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>($"/api/admin/movimientos?clienteId={Guid.NewGuid()}");

        Assert.Empty(movimientos!.Items);
        Assert.Equal("No existen resultados para los filtros seleccionados", movimientos.Mensaje);
    }

    /// <summary>CU-21: el alta de un producto queda auditada con el admin como actor (RN-13).</summary>
    [Fact]
    public async Task ConsultarAuditoria_WithValidFilters_Returns200OK()
    {
        var admin = _factory.CreateClientComo(Roles.Admin);
        var producto = await (await admin.PostAsJsonAsync("/api/admin/productos", new ProductoCreateDTO { Nombre = $"Auditado {Guid.NewGuid():N}", Precio = 100 }))
            .Content.ReadFromJsonAsync<ProductoResponseDTO>();

        var respuesta = await admin.GetAsync("/api/admin/auditoria?tipoOperacion=AdminProductos.CrearProducto");
        var registros = await respuesta.Content.ReadFromJsonAsync<ListadoResponseDTO<AuditoriaResponseDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var registro = Assert.Single(registros!.Items, r => r.EntidadId == producto!.Id);
        Assert.Equal(FidelixAPI.DataAccess.Seed.DbInitializer.AdminDemoId, registro.ActorId);
        Assert.Equal(Roles.Admin, registro.ActorRol);
    }

    /// <summary>CU-21, flujo 3a: sin criterios devuelve 200 con lista vacía y pide acotar la búsqueda.</summary>
    [Fact]
    public async Task ConsultarAuditoria_WithNoFilters_Returns200OKWithEmptyList()
    {
        var registros = await _factory.CreateClientComo(Roles.Admin).GetFromJsonAsync<ListadoResponseDTO<AuditoriaResponseDTO>>("/api/admin/auditoria");

        Assert.Empty(registros!.Items);
        Assert.Equal("Defina al menos un criterio de búsqueda.", registros.Mensaje);
    }

    /// <summary>CU-21, flujo 4a: un actor sin operaciones da 200 con lista vacía y mensaje.</summary>
    [Fact]
    public async Task ConsultarAuditoria_WithNoMatches_Returns200OKWithEmptyList()
    {
        var registros = await _factory.CreateClientComo(Roles.Admin)
            .GetFromJsonAsync<ListadoResponseDTO<AuditoriaResponseDTO>>($"/api/admin/auditoria?actorId={Guid.NewGuid()}");

        Assert.Empty(registros!.Items);
        Assert.Equal("No existen resultados para los filtros seleccionados", registros.Mensaje);
    }

    /// <summary>CU-24: el resumen del período incluye los puntos del seed (200).</summary>
    [Fact]
    public async Task GenerarReporte_WithValidParams_Returns200OK()
    {
        var desde = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var hasta = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync($"/api/admin/reportes?tipo=Resumen&periodoDesde={desde}&periodoHasta={hasta}");
        var reporte = await respuesta.Content.ReadFromJsonAsync<ReporteResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(reporte!.PuntosAcumulados > 0);
    }

    /// <summary>CU-24, flujo 2a: un período invertido devuelve 400.</summary>
    [Fact]
    public async Task GenerarReporte_WithInvalidPeriodRange_Returns400BadRequest()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync("/api/admin/reportes?tipo=Resumen&periodoDesde=2026-12-31&periodoHasta=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-24, flujo 2a: un tipo de reporte que no existe devuelve 400.</summary>
    [Fact]
    public async Task GenerarReporte_WithUnknownType_Returns400BadRequest()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync("/api/admin/reportes?tipo=Inventado&periodoDesde=2026-01-01&periodoHasta=2026-12-31");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-24, flujo 4a: un período sin datos da 200 con el mensaje correspondiente.</summary>
    [Fact]
    public async Task GenerarReporte_WithNoDataInPeriod_Returns200OKWithEmptyReport()
    {
        var reporte = await _factory.CreateClientComo(Roles.Admin)
            .GetFromJsonAsync<ReporteResponseDTO>("/api/admin/reportes?tipo=BeneficiosMasCanjeados&periodoDesde=1990-01-01&periodoHasta=1990-12-31");

        Assert.Equal(0, reporte!.CantidadMovimientos);
        Assert.Equal("No hay datos disponibles para el período seleccionado", reporte.Mensaje);
    }
}
