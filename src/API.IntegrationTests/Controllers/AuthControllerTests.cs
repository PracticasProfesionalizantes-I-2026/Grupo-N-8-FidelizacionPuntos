using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Auth;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de <c>AuthController</c> (CU-02/09/14, CU-25) contra el pipeline HTTP real.</summary>
public class AuthControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    public AuthControllerTests(FidelixApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.InicializarBaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LoginCliente_WithValidCredentials_Returns200OK()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDTO { Email = "cliente.demo@fidelix.local", Password = "Cliente123!" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var body = await respuesta.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task LoginCliente_WithWrongPassword_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDTO { Email = "cliente.demo@fidelix.local", Password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task LoginEmpleado_WithValidCredentials_Returns200OK()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDTO { Email = "empleado.demo@fidelix.local", Password = "Empleado123!" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task LoginAdmin_WithValidCredentials_Returns200OK()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDTO { Email = "admin@fidelix.local", Password = "Admin123!" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task SolicitarRecuperacion_WithUnknownAccount_Returns200OKWithoutLeakingExistence()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/recuperar-contrasena",
            new RecuperarContrasenaDTO { Identificador = "nadie@fidelix.local" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConfirmarRecuperacion_WithInvalidCode_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/auth/recuperar-contrasena/confirmar",
            new ConfirmarRecuperacionDTO { Identificador = "cliente.demo@fidelix.local", Codigo = "000000", NuevaPassword = "NuevaPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
