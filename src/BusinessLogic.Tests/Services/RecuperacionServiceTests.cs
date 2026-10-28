using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests de la recuperación de contraseña (CU-25). </summary>
public class RecuperacionServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();
    private readonly Mock<ICodigoRecuperacionRepository> _codigoRepo = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly RecuperacionService _sut;

    private readonly Cliente _cliente = new() { Id = Guid.NewGuid(), Email = "ana@test.com", Activo = true, IntentosFallidos = 3 };

    /// <summary> Arma el service con un cliente activo existente. </summary>
    public RecuperacionServiceTests()
    {
        _sut = new RecuperacionService(_clienteRepo.Object, _empleadoRepo.Object, _adminRepo.Object, _codigoRepo.Object, _email.Object);
        _clienteRepo.Setup(r => r.GetByEmailAsync(_cliente.Email)).ReturnsAsync(_cliente);
        _codigoRepo.Setup(r => r.CreateAsync(It.IsAny<CodigoRecuperacion>())).ReturnsAsync((CodigoRecuperacion c) => c);
    }

    /// <summary> Deja un código vigente "123456" para el cliente. </summary>
    private CodigoRecuperacion ConfigurarCodigoVigente()
    {
        var codigo = new CodigoRecuperacion { Email = _cliente.Email, Rol = Roles.Cliente, CodigoHash = PasswordHasher.Hash("123456"), FechaExpiracion = DateTime.UtcNow.AddMinutes(10) };
        _codigoRepo.Setup(r => r.GetVigentesPorEmailAsync(_cliente.Email, It.IsAny<DateTime>())).ReturnsAsync([codigo]);
        return codigo;
    }

    /// <summary> CU-25: con una cuenta activa guarda el código hasheado y lo envía por email. </summary>
    [Fact]
    public async Task SolicitarRecuperacionAsync_WithActiveAccount_SendsCode()
    {
        await _sut.SolicitarRecuperacionAsync(new RecuperacionSolicitudDTO { Email = _cliente.Email });

        _codigoRepo.Verify(r => r.CreateAsync(It.Is<CodigoRecuperacion>(c => c.Email == _cliente.Email && c.Rol == Roles.Cliente)), Times.Once);
        _email.Verify(e => e.EnviarAsync(_cliente.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary> CU-25, flujo 3a: con una cuenta inexistente responde igual y no envía nada. </summary>
    [Fact]
    public async Task SolicitarRecuperacionAsync_WithUnknownAccount_DoesNotSendEmail()
    {
        var resultado = await _sut.SolicitarRecuperacionAsync(new RecuperacionSolicitudDTO { Email = "nadie@test.com" });

        Assert.Equal("Si el dato ingresado corresponde a una cuenta, se envió un código", resultado.Mensaje);
        _email.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _codigoRepo.Verify(r => r.CreateAsync(It.IsAny<CodigoRecuperacion>()), Times.Never);
    }

    /// <summary> CU-25, RN-28: el código de un empleado se envía al admin, no al empleado. </summary>
    [Fact]
    public async Task SolicitarRecuperacionAsync_WithEmpleado_SendsCodeToAdmin()
    {
        _empleadoRepo.Setup(r => r.GetByEmailAsync("emp@test.com")).ReturnsAsync(new Empleado { Email = "emp@test.com", Activo = true });
        _adminRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([new Admin { Email = "admin@test.com" }]);

        await _sut.SolicitarRecuperacionAsync(new RecuperacionSolicitudDTO { Email = "emp@test.com" });

        _email.Verify(e => e.EnviarAsync("admin@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _email.Verify(e => e.EnviarAsync("emp@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary> CU-25: con un código válido actualiza la contraseña, quita el bloqueo y marca el código como usado (RN-27). </summary>
    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithValidCode_UpdatesPassword()
    {
        var codigo = ConfigurarCodigoVigente();

        await _sut.ConfirmarRecuperacionAsync(new RecuperacionConfirmacionDTO { Email = _cliente.Email, Codigo = "123456", NuevaPassword = "NuevaClave123" });

        Assert.True(PasswordHasher.Verify("NuevaClave123", _cliente.PasswordHash!));
        Assert.Equal(0, _cliente.IntentosFallidos);
        Assert.True(codigo.Usado);
    }

    /// <summary> CU-25, flujo 5a: un código que no coincide lanza CodigoRecuperacionInvalidoException. </summary>
    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithInvalidCode_ThrowsCodigoRecuperacionInvalidoException()
    {
        ConfigurarCodigoVigente();

        await Assert.ThrowsAsync<CodigoRecuperacionInvalidoException>(() =>
            _sut.ConfirmarRecuperacionAsync(new RecuperacionConfirmacionDTO { Email = _cliente.Email, Codigo = "999999", NuevaPassword = "NuevaClave123" }));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary> CU-25, flujo 5b: una contraseña débil lanza PasswordInvalidaException (RN-02) y el código no se consume. </summary>
    [Fact]
    public async Task ConfirmarRecuperacionAsync_WithWeakPassword_ThrowsPasswordInvalidaException()
    {
        var codigo = ConfigurarCodigoVigente();

        await Assert.ThrowsAsync<PasswordInvalidaException>(() =>
            _sut.ConfirmarRecuperacionAsync(new RecuperacionConfirmacionDTO { Email = _cliente.Email, Codigo = "123456", NuevaPassword = "123" }));
        Assert.False(codigo.Usado);
    }
}
