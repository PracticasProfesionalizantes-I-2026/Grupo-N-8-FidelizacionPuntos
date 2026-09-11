using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Configuration;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="AuthService"/>: login en cascada (CU-02/CU-09/CU-14) y recuperación (CU-25).</summary>
public class AuthServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();
    private readonly Mock<ICodigoRecuperacionRepository> _codigoRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "clave-de-test-suficientemente-larga-1234567890",
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 60
        });

        _sut = new AuthService(
            _clienteRepo.Object, _empleadoRepo.Object, _adminRepo.Object,
            _codigoRepo.Object, _emailSender.Object, _auditoriaService.Object, jwtOptions);
    }

    private static Cliente ClienteConPassword(string password, bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Email = "cliente@fidelix.local",
        Documento = "40111222",
        Nombre = "Cliente Test",
        PasswordHash = PasswordHasher.Hash(password),
        Activo = activo
    };

    [Fact]
    public async Task LoginAsync_WithValidClienteCredentials_ReturnsJwtToken()
    {
        var cliente = ClienteConPassword("Password123!");
        _clienteRepo.Setup(r => r.GetByEmailAsync(cliente.Email)).ReturnsAsync(cliente);

        var resultado = await _sut.LoginAsync(new LoginDTO { Email = cliente.Email, Password = "Password123!" });

        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
        Assert.Equal(RolUsuario.Cliente, resultado.Rol);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsCredencialesInvalidasException()
    {
        var cliente = ClienteConPassword("Password123!");
        _clienteRepo.Setup(r => r.GetByEmailAsync(cliente.Email)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _sut.LoginAsync(new LoginDTO { Email = cliente.Email, Password = "incorrecta" }));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsCredencialesInvalidasException()
    {
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _empleadoRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Empleado?)null);
        _adminRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Admin?)null);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _sut.LoginAsync(new LoginDTO { Email = "nadie@fidelix.local", Password = "cualquiera" }));
    }

    [Fact]
    public async Task LoginAsync_WhenAccountInactive_ThrowsCuentaInactivaException()
    {
        var cliente = ClienteConPassword("Password123!", activo: false);
        _clienteRepo.Setup(r => r.GetByEmailAsync(cliente.Email)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<CuentaInactivaException>(() =>
            _sut.LoginAsync(new LoginDTO { Email = cliente.Email, Password = "Password123!" }));
    }

    [Fact]
    public async Task LoginAsync_AfterFiveFailedAttempts_LocksAccountAndThrowsCuentaBloqueadaException()
    {
        var cliente = ClienteConPassword("Password123!");
        _clienteRepo.Setup(r => r.GetByEmailAsync(cliente.Email)).ReturnsAsync(cliente);

        // RN-03: 5 intentos fallidos consecutivos bloquean la cuenta.
        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
                _sut.LoginAsync(new LoginDTO { Email = cliente.Email, Password = "incorrecta" }));
        }

        await Assert.ThrowsAsync<CuentaBloqueadaException>(() =>
            _sut.LoginAsync(new LoginDTO { Email = cliente.Email, Password = "Password123!" }));
    }

    [Fact]
    public async Task SolicitarRecuperacionAsync_WithKnownCliente_SendsEmailAndCreatesCode()
    {
        var cliente = ClienteConPassword("Password123!");
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(cliente.Documento)).ReturnsAsync(cliente);

        await _sut.SolicitarRecuperacionAsync(new RecuperarContrasenaDTO { Identificador = cliente.Documento });

        _codigoRepo.Verify(r => r.CreateAsync(It.Is<CodigoRecuperacion>(c => c.ClienteId == cliente.Id)), Times.Once);
        _emailSender.Verify(e => e.EnviarAsync(cliente.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarRecuperacionAsync_WithUnknownAccount_DoesNotSendEmail()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _empleadoRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Empleado?)null);
        _empleadoRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Empleado?)null);
        _adminRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Admin?)null);

        await _sut.SolicitarRecuperacionAsync(new RecuperarContrasenaDTO { Identificador = "nadie@fidelix.local" });

        _emailSender.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarRecuperacionAsync_WithEmpleado_SendsCodeToAdminEmail()
    {
        var empleado = new Empleado { Id = Guid.NewGuid(), Documento = "30999888", Email = "empleado@fidelix.local", Nombre = "Empleado Test", Activo = true };
        var admin = new Admin { Id = Guid.NewGuid(), Email = "admin@fidelix.local", Nombre = "Admin", PasswordHash = PasswordHasher.Hash("x"), Activo = true };

        _clienteRepo.Setup(r => r.GetByDocumentoAsync(empleado.Documento)).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.GetByEmailAsync(empleado.Documento)).ReturnsAsync((Cliente?)null);
        _empleadoRepo.Setup(r => r.GetByDocumentoAsync(empleado.Documento)).ReturnsAsync(empleado);
        _adminRepo.Setup(r => r.GetPrimerActivoAsync()).ReturnsAsync(admin);

        await _sut.SolicitarRecuperacionAsync(new RecuperarContrasenaDTO { Identificador = empleado.Documento });

        // RN-28: el código del empleado se envía al email del admin, no al del empleado.
        _emailSender.Verify(e => e.EnviarAsync(admin.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _codigoRepo.Verify(r => r.CreateAsync(It.Is<CodigoRecuperacion>(c => c.EmpleadoId == empleado.Id)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithValidCode_UpdatesPassword()
    {
        var cliente = ClienteConPassword("ViejaPassword1!");
        var codigoPlano = "123456";
        var codigo = new CodigoRecuperacion
        {
            Id = Guid.NewGuid(),
            ClienteId = cliente.Id,
            CodigoHash = PasswordHasher.Hash(codigoPlano),
            FechaExpiracion = DateTime.UtcNow.AddMinutes(10),
            Usado = false
        };

        _clienteRepo.Setup(r => r.GetByDocumentoAsync(cliente.Documento)).ReturnsAsync(cliente);
        _codigoRepo.Setup(r => r.GetVigentesPorClienteAsync(cliente.Id, It.IsAny<DateTime>())).ReturnsAsync([codigo]);

        await _sut.ConfirmarRecuperacionAsync(new ConfirmarRecuperacionDTO
        {
            Identificador = cliente.Documento,
            Codigo = codigoPlano,
            NuevaPassword = "NuevaPassword1!"
        });

        Assert.True(PasswordHasher.Verify("NuevaPassword1!", cliente.PasswordHash!));
        Assert.True(codigo.Usado);
    }

    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithInvalidCode_ThrowsCodigoRecuperacionInvalidoException()
    {
        var cliente = ClienteConPassword("ViejaPassword1!");
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(cliente.Documento)).ReturnsAsync(cliente);
        _codigoRepo.Setup(r => r.GetVigentesPorClienteAsync(cliente.Id, It.IsAny<DateTime>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<CodigoRecuperacionInvalidoException>(() =>
            _sut.ConfirmarRecuperacionAsync(new ConfirmarRecuperacionDTO
            {
                Identificador = cliente.Documento,
                Codigo = "000000",
                NuevaPassword = "NuevaPassword1!"
            }));
    }

    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithWeakPassword_ThrowsPasswordInvalidaException()
    {
        await Assert.ThrowsAsync<PasswordInvalidaException>(() =>
            _sut.ConfirmarRecuperacionAsync(new ConfirmarRecuperacionDTO
            {
                Identificador = "cualquiera",
                Codigo = "000000",
                NuevaPassword = "123"
            }));
    }
}
