using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="ClienteService"/> (CU-01, CU-03, CU-12).</summary>
public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly ClienteService _sut;

    public ClienteServiceTests()
    {
        _sut = new ClienteService(_clienteRepo.Object, _auditoriaService.Object);
    }

    [Fact]
    public async Task RegistrarAsync_WithValidData_CreatesAndReturnsCliente()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => c);

        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "Password123!" };
        var resultado = await _sut.RegistrarAsync(dto);

        Assert.Equal(dto.Email, resultado.Email);
        Assert.True(resultado.Activo);
    }

    [Fact]
    public async Task RegistrarAsync_WithWeakPassword_ThrowsPasswordInvalidaException()
    {
        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "123" };

        await Assert.ThrowsAsync<PasswordInvalidaException>(() => _sut.RegistrarAsync(dto));
    }

    [Fact]
    public async Task RegistrarAsync_WhenDocumentoOrEmailExists_ThrowsClienteDuplicadoException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync(new Cliente { Documento = "40555666" });

        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "Password123!" };

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => _sut.RegistrarAsync(dto));
    }

    [Fact]
    public async Task CrearDesdePOSAsync_WithValidData_CreatesClienteWithoutPassword()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        Cliente? creado = null;
        _clienteRepo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => { creado = c; return c; });

        var dto = new ClienteCreateDTO { Nombre = "Beto", Documento = "40777888", Email = "beto@test.com" };
        await _sut.CrearDesdePOSAsync(dto);

        Assert.Null(creado!.PasswordHash);
    }

    [Fact]
    public async Task ActualizarPerfilAsync_WithValidData_UpdatesAndReturnsCliente()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        var resultado = await _sut.ActualizarPerfilAsync(cliente.Id, new ClienteUpdateDTO { Telefono = "1122334455" });

        Assert.Equal("1122334455", resultado.Telefono);
    }

    [Fact]
    public async Task ActualizarPerfilAsync_WhenDocumentoChanged_ThrowsValidationException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Documento = "40555666", Email = "ana@test.com", Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ActualizarPerfilAsync(cliente.Id, new ClienteUpdateDTO { Documento = "99999999" }));
    }

    [Fact]
    public async Task ActualizarPerfilAsync_WhenEmailAlreadyExists_ThrowsEmailDuplicadoException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Documento = "40555666", Email = "ana@test.com", Activo = true };
        var otro = new Cliente { Id = Guid.NewGuid(), Email = "ocupado@test.com" };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);
        _clienteRepo.Setup(r => r.GetByEmailAsync(otro.Email)).ReturnsAsync(otro);

        await Assert.ThrowsAsync<EmailDuplicadoException>(() =>
            _sut.ActualizarPerfilAsync(cliente.Id, new ClienteUpdateDTO { Email = otro.Email }));
    }

    [Fact]
    public async Task DarseDeBajaAsync_WithAuthenticatedCliente_DeactivatesCliente()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        await _sut.DarseDeBajaAsync(cliente.Id);

        Assert.False(cliente.Activo);
    }

    [Fact]
    public async Task DarseDeBajaAsync_WithUnknownCliente_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.DarseDeBajaAsync(Guid.NewGuid()));
    }
}
