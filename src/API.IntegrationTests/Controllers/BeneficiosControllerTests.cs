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

/// <summary>Tests de integración del catálogo de beneficios (`BeneficiosController`, CU-06).</summary>
public class BeneficiosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public BeneficiosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>CU-06: un beneficio activo aparece en el catálogo y uno desactivado no (RN-07).</summary>
    [Fact]
    public async Task ListarBeneficios_Returns200OKWithActiveList()
    {
        var admin = _factory.CreateClientComo(Roles.Admin);
        var activo = await (await admin.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = $"Activo {Guid.NewGuid():N}", CostoPuntos = 10 }))
            .Content.ReadFromJsonAsync<BeneficioResponseDTO>();
        var inactivo = await (await admin.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = $"Inactivo {Guid.NewGuid():N}", CostoPuntos = 10 }))
            .Content.ReadFromJsonAsync<BeneficioResponseDTO>();
        await admin.PatchAsync($"/api/admin/beneficios/{inactivo!.Id}/desactivar", null);

        var respuesta = await _factory.CreateClientComo(Roles.Cliente).GetAsync("/api/beneficios");
        var catalogo = await respuesta.Content.ReadFromJsonAsync<ListadoResponseDTO<BeneficioResponseDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(catalogo!.Items, b => b.Id == activo!.Id);
        Assert.DoesNotContain(catalogo.Items, b => b.Id == inactivo.Id);
    }

    /// <summary>CU-06, flujo 4a: si se desactivan todos, el catálogo responde 200 con lista vacía y mensaje.</summary>
    [Fact]
    public async Task ListarBeneficios_WithNoActiveBeneficios_Returns200OKWithEmptyList()
    {
        var admin = _factory.CreateClientComo(Roles.Admin);
        var cliente = _factory.CreateClientComo(Roles.Cliente);
        var actuales = await cliente.GetFromJsonAsync<ListadoResponseDTO<BeneficioResponseDTO>>("/api/beneficios");
        foreach (var b in actuales!.Items)
        {
            await admin.PatchAsync($"/api/admin/beneficios/{b.Id}/desactivar", null);
        }

        var catalogo = await cliente.GetFromJsonAsync<ListadoResponseDTO<BeneficioResponseDTO>>("/api/beneficios");

        Assert.Empty(catalogo!.Items);
        Assert.Equal("No hay beneficios disponibles", catalogo.Mensaje);
    }

    /// <summary>CU-06, precondición: sin token devuelve 401.</summary>
    [Fact]
    public async Task ListarBeneficios_WithoutToken_Returns401Unauthorized()
    {
        var respuesta = await _factory.CreateClient().GetAsync("/api/beneficios");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
