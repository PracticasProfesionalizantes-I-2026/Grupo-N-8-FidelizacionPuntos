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
/// Tests de integración de `AuthPersonalController`: login de empleado (CU-09), de admin (CU-14) y
/// recuperación de contraseña (CU-25). Los emails se guardan en memoria para leer el código.
/// </summary>
public class AuthPersonalControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly EmailSenderEnMemoria _emails = new();
    private readonly FidelixApiFactory _factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Recibe la factory compartida y reemplaza el envío de emails por uno en memoria.</summary>
    public AuthPersonalControllerTests(FidelixApiFactory factory)
    {
        _factory = factory;
        _cliente = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IEmailSender>(_emails))).CreateClient();
    }

    /// <summary>Cliente HTTP de la API con el email en memoria.</summary>
    private readonly HttpClient _cliente;

    /// <summary>Hace el login de empleado con las credenciales indicadas.</summary>
    private Task<HttpResponseMessage> LoginEmpleado(string email, string password) =>
        _cliente.PostAsJsonAsync("/api/auth/empleados/login", new LoginRequestDTO { Email = email, Password = password });

    /// <summary>Hace el login de admin con las credenciales indicadas.</summary>
    private Task<HttpResponseMessage> LoginAdmin(string email, string password) =>
        _cliente.PostAsJsonAsync("/api/auth/admin/login", new LoginRequestDTO { Email = email, Password = password });

    /// <summary>CU-09: el empleado sembrado entra y recibe un token con rol Empleado.</summary>
    [Fact]
    public async Task LoginEmpleado_WithValidCredentials_Returns200OK()
    {
        var respuesta = await LoginEmpleado("empleado@fidelix.local", "Empleado123!");
        var login = await respuesta.Content.ReadFromJsonAsync<LoginPersonalResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Roles.Empleado, login!.Rol);
        Assert.False(string.IsNullOrEmpty(login.Token));
    }

    /// <summary>CU-09, flujo 2a: sin contraseña devuelve 400.</summary>
    [Fact]
    public async Task LoginEmpleado_WithMissingField_Returns400BadRequest()
    {
        var respuesta = await LoginEmpleado("empleado@fidelix.local", "");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-09, flujo 3a: contraseña incorrecta devuelve 401.</summary>
    [Fact]
    public async Task LoginEmpleado_WithWrongPassword_Returns401Unauthorized()
    {
        var respuesta = await LoginEmpleado("empleado@fidelix.local", "Incorrecta1");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>CU-09, flujo 3b: el empleado dado de baja sembrado recibe 403 (RN-15).</summary>
    [Fact]
    public async Task LoginEmpleado_WhenAccountInactive_Returns403Forbidden()
    {
        var respuesta = await LoginEmpleado("empleado.inactivo@fidelix.local", "Inactivo123!");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>CU-14: el admin sembrado entra y su token abre los endpoints de admin (RN-11).</summary>
    [Fact]
    public async Task LoginAdmin_WithValidCredentials_Returns200OK()
    {
        var respuesta = await LoginAdmin("admin@fidelix.local", "Admin123!");
        var login = await respuesta.Content.ReadFromJsonAsync<LoginPersonalResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(Roles.Admin, login!.Rol);

        _cliente.DefaultRequestHeaders.Authorization = new("Bearer", login.Token);
        var reglas = await _cliente.GetAsync("/api/admin/reglas-acumulacion");
        _cliente.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, reglas.StatusCode);
    }

    /// <summary>CU-14, flujo 2a: sin email devuelve 400.</summary>
    [Fact]
    public async Task LoginAdmin_WithMissingField_Returns400BadRequest()
    {
        var respuesta = await LoginAdmin("", "Admin123!");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-14, flujo 3a: contraseña incorrecta devuelve 401.</summary>
    [Fact]
    public async Task LoginAdmin_WithWrongPassword_Returns401Unauthorized()
    {
        var respuesta = await LoginAdmin("admin@fidelix.local", "Incorrecta1");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>RN-11: un endpoint de admin sin token devuelve 401 y con token de cliente 403.</summary>
    [Fact]
    public async Task EndpointAdmin_WithoutTokenOrWrongRole_Returns401Or403()
    {
        var sinToken = await _factory.CreateClient().GetAsync("/api/admin/reglas-acumulacion");
        var conCliente = await _factory.CreateClientComo(Roles.Cliente).GetAsync("/api/admin/reglas-acumulacion");

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, conCliente.StatusCode);
    }

    /// <summary>CU-25: un cliente nuevo pide el código, lo recibe por email, cambia la contraseña y entra con la nueva.</summary>
    [Fact]
    public async Task RecuperarContrasena_WithValidCode_Returns200OK()
    {
        var (_, _, email) = await TestHelpers.RegistrarYAutenticarClienteAsync(_factory.CreateClient());

        await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new RecuperacionSolicitudDTO { Email = email });
        var codigo = _emails.UltimoCodigoPara(email);
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena/confirmar",
            new RecuperacionConfirmacionDTO { Email = email, Codigo = codigo!, NuevaPassword = "NuevaClave123" });
        var login = await _cliente.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = "NuevaClave123" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    /// <summary>CU-25, flujo 3a: con un email que no existe responde 200 igual y no envía nada.</summary>
    [Fact]
    public async Task SolicitarRecuperacion_WithUnknownAccount_Returns200OKWithoutLeakingExistence()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new RecuperacionSolicitudDTO { Email = "nadie@fidelix.local" });
        var mensaje = await respuesta.Content.ReadFromJsonAsync<MensajeResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Si el dato ingresado corresponde a una cuenta, se envió un código", mensaje!.Mensaje);
        Assert.Null(_emails.UltimoCodigoPara("nadie@fidelix.local"));
    }

    /// <summary>CU-25, flujo 5a: un código incorrecto devuelve 400.</summary>
    [Fact]
    public async Task ConfirmarRecuperacion_WithInvalidCode_Returns400BadRequest()
    {
        var (_, _, email) = await TestHelpers.RegistrarYAutenticarClienteAsync(_factory.CreateClient());
        await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new RecuperacionSolicitudDTO { Email = email });
        var codigoIncorrecto = _emails.UltimoCodigoPara(email) == "000000" ? "111111" : "000000";

        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena/confirmar",
            new RecuperacionConfirmacionDTO { Email = email, Codigo = codigoIncorrecto, NuevaPassword = "NuevaClave123" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-25, flujo 5b: con el código correcto pero una contraseña débil devuelve 400.</summary>
    [Fact]
    public async Task ConfirmarRecuperacion_WithWeakPassword_Returns400BadRequest()
    {
        var (_, _, email) = await TestHelpers.RegistrarYAutenticarClienteAsync(_factory.CreateClient());
        await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new RecuperacionSolicitudDTO { Email = email });

        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena/confirmar",
            new RecuperacionConfirmacionDTO { Email = email, Codigo = _emails.UltimoCodigoPara(email)!, NuevaPassword = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-25, RN-28: el código de un empleado le llega al admin, no al empleado.</summary>
    [Fact]
    public async Task SolicitarRecuperacion_WithEmpleado_SendsCodeToAdmin()
    {
        await _cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new RecuperacionSolicitudDTO { Email = "empleado@fidelix.local" });

        Assert.NotNull(_emails.UltimoCodigoPara("admin@fidelix.local"));
        Assert.Null(_emails.UltimoCodigoPara("empleado@fidelix.local"));
    }
}
