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

/// <summary> Tests del login de empleado (CU-09) y admin (CU-14). </summary>
public class AuthPersonalServiceTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();
    private readonly AuthPersonalService _sut;

    private readonly Empleado _empleado = new() { Id = Guid.NewGuid(), Nombre = "Emp", Email = "emp@test.com", PasswordHash = PasswordHasher.Hash("Empleado123!"), Activo = true };
    private readonly Admin _admin = new() { Id = Guid.NewGuid(), Nombre = "Adm", Email = "adm@test.com", PasswordHash = PasswordHasher.Hash("Admin123!") };

    /// <summary> Arma el service con un empleado y un admin existentes y un token de prueba. </summary>
    public AuthPersonalServiceTests()
    {
        _sut = new AuthPersonalService(_tokenService.Object, _empleadoRepo.Object, _adminRepo.Object);
        _empleadoRepo.Setup(r => r.GetByEmailAsync(_empleado.Email)).ReturnsAsync(_empleado);
        _adminRepo.Setup(r => r.GetByEmailAsync(_admin.Email)).ReturnsAsync(_admin);
        _tokenService.Setup(t => t.GenerarToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("token-de-prueba");
    }

    /// <summary> CU-09: credenciales válidas devuelven el token con rol Empleado (RN-11). </summary>
    [Fact]
    public async Task LoginEmpleadoAsync_WithValidCredentials_ReturnsJwtToken()
    {
        var resultado = await _sut.LoginEmpleadoAsync(new LoginRequestDTO { Email = _empleado.Email, Password = "Empleado123!" });

        Assert.Equal("token-de-prueba", resultado.Token);
        Assert.Equal(Roles.Empleado, resultado.Rol);
        _tokenService.Verify(t => t.GenerarToken(_empleado.Id, Roles.Empleado), Times.Once);
    }

    /// <summary> CU-09, flujo 3a: contraseña incorrecta lanza CredencialesInvalidasException. </summary>
    [Fact]
    public async Task LoginEmpleadoAsync_WithWrongPassword_ThrowsCredencialesInvalidasException()
    {
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _sut.LoginEmpleadoAsync(new LoginRequestDTO { Email = _empleado.Email, Password = "Incorrecta" }));
    }

    /// <summary> CU-09, flujo 3b: un empleado dado de baja no puede entrar (RN-15). </summary>
    [Fact]
    public async Task LoginEmpleadoAsync_WhenAccountInactive_ThrowsCuentaInactivaException()
    {
        _empleado.Activo = false;

        await Assert.ThrowsAsync<CuentaInactivaException>(() =>
            _sut.LoginEmpleadoAsync(new LoginRequestDTO { Email = _empleado.Email, Password = "Empleado123!" }));
        _tokenService.Verify(t => t.GenerarToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary> CU-14: credenciales válidas devuelven el token con rol Admin (RN-11). </summary>
    [Fact]
    public async Task LoginAdminAsync_WithValidCredentials_ReturnsJwtToken()
    {
        var resultado = await _sut.LoginAdminAsync(new LoginRequestDTO { Email = _admin.Email, Password = "Admin123!" });

        Assert.Equal(Roles.Admin, resultado.Rol);
        _tokenService.Verify(t => t.GenerarToken(_admin.Id, Roles.Admin), Times.Once);
    }

    /// <summary> CU-14, flujo 3a: contraseña incorrecta lanza CredencialesInvalidasException. </summary>
    [Fact]
    public async Task LoginAdminAsync_WithWrongPassword_ThrowsCredencialesInvalidasException()
    {
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _sut.LoginAdminAsync(new LoginRequestDTO { Email = _admin.Email, Password = "Incorrecta" }));
    }
}
