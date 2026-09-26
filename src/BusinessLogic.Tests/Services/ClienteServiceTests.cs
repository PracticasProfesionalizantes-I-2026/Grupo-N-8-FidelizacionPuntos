using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="ClienteService"/> (CU-01, CU-04). Los repositorios son mocks: nunca se toca la base.</summary>
public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly ClienteService _sut;

    /// <summary>Arma el service bajo prueba con los repositorios mockeados.</summary>
    public ClienteServiceTests()
    {
        _sut = new ClienteService(_clienteRepo.Object, _movimientoRepo.Object);
    }

    /// <summary>CU-01, flujo principal: datos válidos crean el cliente y lo devuelven como DTO.</summary>
    [Fact]
    public async Task RegistrarClienteAsync_WithValidData_CreatesAndReturnsCliente()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _clienteRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        Cliente? creado = null;
        _clienteRepo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => { creado = c; return c; });

        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "Password123!" };
        var resultado = await _sut.RegistrarAsync(dto);

        Assert.Equal(dto.Email, resultado.Email);
        Assert.True(resultado.Activo);
        Assert.NotEqual(dto.Password, creado!.PasswordHash); // Se guarda el hash, nunca la contraseña.
    }

    /// <summary>CU-01, flujo 3b: una contraseña débil lanza <see cref="PasswordInvalidaException"/>.</summary>
    [Fact]
    public async Task RegistrarClienteAsync_WithWeakPassword_ThrowsPasswordInvalidaException()
    {
        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "123" };

        await Assert.ThrowsAsync<PasswordInvalidaException>(() => _sut.RegistrarAsync(dto));
    }

    /// <summary>CU-01, flujo 3a: documento ya registrado lanza <see cref="ClienteDuplicadoException"/> y no crea nada.</summary>
    [Fact]
    public async Task RegistrarClienteAsync_WhenDocumentoOrEmailExists_ThrowsClienteDuplicadoException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync(new Cliente { Documento = "40555666" });

        var dto = new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Password = "Password123!" };

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => _sut.RegistrarAsync(dto));
        _clienteRepo.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary>CU-04, flujo principal: el saldo es la suma de lo disponible en los lotes vigentes que devuelve el repositorio.</summary>
    [Fact]
    public async Task ConsultarSaldoAsync_WithExpiredAndActiveLotes_ReturnsNetBalance()
    {
        var clienteId = Guid.NewGuid();
        _clienteRepo.Setup(r => r.GetByIdAsync(clienteId)).ReturnsAsync(new Cliente { Id = clienteId });
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(clienteId, It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Movimiento { Tipo = TipoMovimiento.Acumulacion, Puntos = 100, PuntosDisponibles = 100 },
                new Movimiento { Tipo = TipoMovimiento.Acumulacion, Puntos = 80, PuntosDisponibles = 30 }
            ]);

        var resultado = await _sut.ConsultarSaldoAsync(clienteId);

        Assert.Equal(130, resultado.PuntosDisponibles);
    }

    /// <summary>CU-04, flujo 4a: si el repositorio falla, se traduce a <see cref="PersistenceException"/>.</summary>
    [Fact]
    public async Task ConsultarSaldoAsync_WhenRepositoryFails_ThrowsPersistenceException()
    {
        var clienteId = Guid.NewGuid();
        _clienteRepo.Setup(r => r.GetByIdAsync(clienteId)).ReturnsAsync(new Cliente { Id = clienteId });
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(clienteId, It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("Falla de conexión simulada"));

        await Assert.ThrowsAsync<PersistenceException>(() => _sut.ConsultarSaldoAsync(clienteId));
    }

    /// <summary>CU-04 (temporal, mientras el Id viaja en la ruta): un cliente inexistente lanza <see cref="ClienteNotFoundException"/>.</summary>
    [Fact]
    public async Task ConsultarSaldoAsync_WithNonExistentCliente_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.ConsultarSaldoAsync(Guid.NewGuid()));
    }
}
