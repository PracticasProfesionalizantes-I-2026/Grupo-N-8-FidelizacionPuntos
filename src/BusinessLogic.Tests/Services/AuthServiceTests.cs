using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.VisualBasic;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="AuthService"/> (CU-02). Las dependencias son mocks: nunca se toca la base de datos.</summary>
public class AuthServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    /// <summary>Arma el service bajo prueba con las dependencias mockeadas.</summary>
    public AuthServiceTests()
    {
        _sut = new AuthService(_tokenService.Object, _clienteRepo.Object);
    }

    /// <summary>(CU-02), Flujo 3c: Cuenta Inactiva lanza CuentaInactivaException</summary>
    [Fact]
    public async Task LoginClienteAsync_WhenAccountInactive_ThrowsCuentaInactivaException()
    {
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new Cliente { Activo = false });

        var request = new LoginRequestDTO { Email = "cuentaInactivaTest@Fidelix.com", Password = "cliente123!" };

        await Assert.ThrowsAsync<CuentaInactivaException>(() => _sut.LoginClienteAsync(request));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never());
    }

    /// <summary>(CU-02), Flujo 3a: Credenciales Invalidas lanza CredencialesInvalidasException</summary>
    [Fact]
    public async Task LoginClienteAsync_WithWrongPassword_ThrowsCredencialesInvalidasException()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Email = "demo@fidelix.local",
            PasswordHash = PasswordHasher.Hash("cliente1234!"),
            Activo = true
        };
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(cliente);

        var request = new LoginRequestDTO { Email = "demo@fidelix.local", Password = "OtraClave123!" };

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _sut.LoginClienteAsync(request));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Once());
        Assert.Equal(1, cliente.IntentosFallidos);
    }

    /// <summary>(CU-02), Flujo 3b: Cuenta bloqueada lanza CuentaBloqueadaException</summary>
    [Fact]
    public async Task LoginClienteAsync_WhenAccountLocked_ThrowsCuentaBloqueadaException()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Email = "demo@fidelix.local",
            Activo = true,
            BloqueadoHasta = DateTime.UtcNow.AddMinutes(30)
        };

        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(cliente);

        var request = new LoginRequestDTO { Email = "demo@fidelix.local", Password = "cliente123!" };

        await Assert.ThrowsAsync<CuentaBloqueadaException>(() => _sut.LoginClienteAsync(request));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never());
    }

    /// <summary>(CU-02), Flujo Principal: Login exitoso devuelve el token y el cliente, y resetea RN-03</summary>
    [Fact]
    public async Task LoginClienteAsync_WithValidCredentials_ReturnsJwtToken()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Email = "demo@fidelix.local",
            PasswordHash = PasswordHasher.Hash("cliente123!"),
            IntentosFallidos = 2,
            BloqueadoHasta = DateTime.UtcNow.AddMinutes(-5),
            Activo = true
        };

        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(cliente);
        _tokenService.Setup(t => t.GenerarToken(cliente.Id, "Cliente")).Returns("Jwt");

        var request = new LoginRequestDTO { Email = "demo@fidelix.local", Password = "cliente123!" };
        var response = await _sut.LoginClienteAsync(request);

        Assert.NotNull(response);
        Assert.Equal("Jwt", response.Token);
        Assert.Equal(cliente.Email, response.Cliente.Email);
        _clienteRepo.Verify(r => r.UpdateAsync(It.Is<Cliente>(c => c.IntentosFallidos == 0 && c.BloqueadoHasta == null)), Times.Once());
    }
}
