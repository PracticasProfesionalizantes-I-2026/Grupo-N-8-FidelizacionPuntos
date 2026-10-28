using System.Net;
using System.Net.Http.Json;
using FidelixAPI.API.IntegrationTests.Fakes;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Seed;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración de los endpoints del cliente autenticado (`/api/clientes/me`: CU-03, CU-04,
/// CU-05, CU-08) y de los del empleado en `ClientesController` (CU-12, CU-13).
/// </summary>
public class ClientesCuentaControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public ClientesCuentaControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>CU-04: un cliente con un lote de 200 puntos consulta su propio saldo con su token.</summary>
    [Fact]
    public async Task ConsultarMiSaldo_WithClienteToken_Returns200OK()
    {
        var client = _factory.CreateClient();
        var (id, _, _) = await TestHelpers.RegistrarYAutenticarClienteAsync(client);
        await _factory.AcreditarPuntosAsync(id, 200);

        var saldo = await client.GetFromJsonAsync<SaldoResponseDTO>("/api/clientes/me/saldo");

        Assert.Equal(200, saldo!.PuntosDisponibles);
    }

    /// <summary>CU-03: el cliente modifica su nombre y lo ve en la respuesta.</summary>
    [Fact]
    public async Task ActualizarPerfil_WithValidData_Returns200OK()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var respuesta = await client.PutAsJsonAsync("/api/clientes/me", new ClienteUpdateDTO { Nombre = "Nombre Nuevo" });
        var perfil = await client.GetFromJsonAsync<ClienteResponseDTO>("/api/clientes/me");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Nombre Nuevo", perfil!.Nombre);
    }

    /// <summary>CU-03, flujo 2a: un email con formato inválido devuelve 400.</summary>
    [Fact]
    public async Task ActualizarPerfil_WithInvalidField_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var respuesta = await client.PutAsJsonAsync("/api/clientes/me", new ClienteUpdateDTO { Email = "no-es-un-email" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-03, flujo 2b: intentar cambiar el documento devuelve 400 (RN-04).</summary>
    [Fact]
    public async Task ActualizarPerfil_WhenDocumentoChanged_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var respuesta = await client.PutAsJsonAsync("/api/clientes/me", new ClienteUpdateDTO { Documento = "11111111" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-03, flujo 3a: el email del cliente demo ya está tomado (409, RN-01).</summary>
    [Fact]
    public async Task ActualizarPerfil_WhenEmailAlreadyExists_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var respuesta = await client.PutAsJsonAsync("/api/clientes/me", new ClienteUpdateDTO { Email = "cliente.demo@fidelix.local" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-03, baja: devuelve 204 y después el login lo rechaza por cuenta inactiva (RN-14).</summary>
    [Fact]
    public async Task DarseDeBaja_WithAuthenticatedCliente_Returns204NoContent()
    {
        var client = _factory.CreateClient();
        var (_, _, email) = await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var respuesta = await client.DeleteAsync("/api/clientes/me");
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = "Cliente123!" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    /// <summary>CU-05: el cliente demo ve sus movimientos filtrados por tipo.</summary>
    [Fact]
    public async Task ConsultarHistorial_WithValidFilters_Returns200OK()
    {
        var client = _factory.CreateClient();
        await TestHelpers.LoginClienteAsync(client, "cliente.demo@fidelix.local", "Cliente123!");

        var respuesta = await client.GetAsync("/api/clientes/me/movimientos?tipoMovimiento=Acumulacion");
        var historial = await respuesta.Content.ReadFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotEmpty(historial!.Items);
        Assert.All(historial.Items, m => Assert.Equal("Acumulacion", m.Tipo));
    }

    /// <summary>CU-05, flujo 2a: una fecha mal formada devuelve 400.</summary>
    [Fact]
    public async Task ConsultarHistorial_WithInvalidDateFilter_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelpers.LoginClienteAsync(client, "cliente.demo@fidelix.local", "Cliente123!");

        var respuesta = await client.GetAsync("/api/clientes/me/movimientos?fechaDesde=no-es-una-fecha");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-05, flujo 4a: un cliente nuevo no tiene movimientos: 200 con lista vacía y mensaje.</summary>
    [Fact]
    public async Task ConsultarHistorial_WithNoMovimientos_Returns200OKWithEmptyList()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var historial = await client.GetFromJsonAsync<ListadoResponseDTO<MovimientoResponseDTO>>("/api/clientes/me/movimientos");

        Assert.Empty(historial!.Items);
        Assert.Equal("No hay movimientos para mostrar", historial.Mensaje);
    }

    /// <summary>CU-08: después de un canje en el local, el cliente lo ve con estado "Entregado" (RN-10).</summary>
    [Fact]
    public async Task ConsultarCanjes_WithValidToken_Returns200OK()
    {
        var client = _factory.CreateClient();
        var (id, documento, _) = await TestHelpers.RegistrarYAutenticarClienteAsync(client);
        await _factory.AcreditarPuntosAsync(id, 1000);
        var beneficios = await client.GetFromJsonAsync<ListadoResponseDTO<FidelixAPI.Shared.DTOs.Beneficio.BeneficioResponseDTO>>("/api/beneficios");
        var masBarato = beneficios!.Items.OrderBy(b => b.CostoPuntos).First();
        await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/canjes/presencial",
            new CanjePresencialCreateDTO { ClienteDocumento = documento, BeneficioId = masBarato.Id });

        var canjes = await client.GetFromJsonAsync<ListadoResponseDTO<CanjeItemDTO>>("/api/clientes/me/canjes");

        Assert.Contains(canjes!.Items, c => c.BeneficioId == masBarato.Id && c.Estado == "Entregado");
    }

    /// <summary>CU-08, flujo 4a: un cliente sin canjes recibe 200 con lista vacía y mensaje.</summary>
    [Fact]
    public async Task ConsultarCanjes_WithNoCanjes_Returns200OKWithEmptyList()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegistrarYAutenticarClienteAsync(client);

        var canjes = await client.GetFromJsonAsync<ListadoResponseDTO<CanjeItemDTO>>("/api/clientes/me/canjes");

        Assert.Empty(canjes!.Items);
        Assert.Equal("Aún no realizaste ningún canje", canjes.Mensaje);
    }

    /// <summary>CU-12: el empleado da de alta un cliente sin contraseña (201).</summary>
    [Fact]
    public async Task CrearClientePOS_WithValidData_Returns201Created()
    {
        var documento = TestHelpers.DocumentoUnico();

        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/clientes",
            new ClienteCreateDTO { Nombre = "Cliente POS", Documento = documento, Email = $"pos.{documento}@fidelix.local" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    /// <summary>CU-12, flujo 2a: sin documento devuelve 400.</summary>
    [Fact]
    public async Task CrearClientePOS_WithInvalidData_Returns400BadRequest()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/clientes",
            new ClienteCreateDTO { Nombre = "Cliente POS", Documento = "", Email = "pos@fidelix.local" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-12, flujo 3a: el documento del cliente demo ya existe (409, RN-01).</summary>
    [Fact]
    public async Task CrearClientePOS_WhenDocumentoExists_Returns409Conflict()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).PostAsJsonAsync("/api/clientes",
            new ClienteCreateDTO { Nombre = "Repetido", Documento = "40333444", Email = "repetido@fidelix.local" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-13: el empleado consulta los canjes del cliente demo por documento (200).</summary>
    [Fact]
    public async Task ConsultarCanjesPorDocumento_WithValidDocumento_Returns200OK()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).GetAsync("/api/clientes/40333444/canjes");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    /// <summary>CU-13, flujo 3a: un documento que no existe devuelve 404.</summary>
    [Fact]
    public async Task ConsultarCanjesPorDocumento_WithUnknownDocumento_Returns404NotFound()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Empleado).GetAsync("/api/clientes/99999999/canjes");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-13, flujo 4a: un cliente recién creado no tiene canjes: 200 con lista vacía.</summary>
    [Fact]
    public async Task ConsultarCanjesPorDocumento_WithNoCanjes_Returns200OKWithEmptyList()
    {
        var empleado = _factory.CreateClientComo(Roles.Empleado);
        var documento = TestHelpers.DocumentoUnico();
        await empleado.PostAsJsonAsync("/api/clientes", new ClienteCreateDTO { Nombre = "Sin Canjes", Documento = documento, Email = $"sc.{documento}@fidelix.local" });

        var canjes = await empleado.GetFromJsonAsync<ListadoResponseDTO<CanjeItemDTO>>($"/api/clientes/{documento}/canjes");

        Assert.Empty(canjes!.Items);
        Assert.Equal("El cliente aún no registra beneficios canjeados", canjes.Mensaje);
    }
}
