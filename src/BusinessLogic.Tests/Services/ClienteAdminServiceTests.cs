using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests del ABM de clientes por el admin (CU-15). </summary>
public class ClienteAdminServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly ClienteAdminService _sut;

    /// <summary> Arma el service con el repositorio mockeado. </summary>
    public ClienteAdminServiceTests() => _sut = new ClienteAdminService(_clienteRepo.Object);

    /// <summary> CU-15, alta: datos válidos crean el cliente activo. </summary>
    [Fact]
    public async Task CrearClienteAsync_WithValidData_CreatesAndReturnsCliente()
    {
        _clienteRepo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => c);

        var resultado = await _sut.CrearClienteAsync(new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com" });

        Assert.True(resultado.Activo);
        Assert.Equal("40555666", resultado.Documento);
    }

    /// <summary> CU-15, flujo 3a: un documento ya registrado lanza ClienteDuplicadoException (RN-01). </summary>
    [Fact]
    public async Task CrearClienteAsync_WhenDocumentoExists_ThrowsClienteDuplicadoException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync("40555666")).ReturnsAsync(new Cliente());

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() =>
            _sut.CrearClienteAsync(new ClienteCreateDTO { Nombre = "Ana", Documento = "40555666", Email = "ana@test.com" }));
        _clienteRepo.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary> CU-15, modificación: cambia lo enviado; el admin puede pasar al cliente a Gold (N1). </summary>
    [Fact]
    public async Task ActualizarClienteAsync_WithValidData_UpdatesAndReturnsCliente()
    {
        var cliente = new Cliente { Id = IdsDePrueba.Nuevo(), Nombre = "Ana", Email = "ana@test.com", Categoria = CategoriaCliente.Estandar };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        var resultado = await _sut.ActualizarClienteAsync(cliente.Id, new AdminClienteUpdateDTO { Categoria = CategoriaCliente.Gold });

        Assert.Equal(CategoriaCliente.Gold, resultado.Categoria);
        Assert.Equal("Ana", resultado.Nombre);
        _clienteRepo.Verify(r => r.UpdateAsync(cliente), Times.Once);
    }

    /// <summary> CU-15, flujo 3b: un id inexistente lanza ClienteNotFoundException. </summary>
    [Fact]
    public async Task ActualizarClienteAsync_WithNonExistentId_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() =>
            _sut.ActualizarClienteAsync(IdsDePrueba.Nuevo(), new AdminClienteUpdateDTO { Nombre = "X" }));
    }

    /// <summary> CU-15, baja: el cliente queda inactivo (baja lógica, RN-14). </summary>
    [Fact]
    public async Task BajaClienteAsync_WithExistingId_DeactivatesCliente()
    {
        var cliente = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        await _sut.BajaClienteAsync(cliente.Id);

        Assert.False(cliente.Activo);
        _clienteRepo.Verify(r => r.UpdateAsync(cliente), Times.Once);
    }

    /// <summary> CU-15, listar: devuelve los clientes como `{ mensaje, items }` (mensaje nulo si hay datos). </summary>
    [Fact]
    public async Task ListarClientesAsync_ReturnsListadoWithItems()
    {
        _clienteRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([new Cliente { Id = IdsDePrueba.Nuevo(), Nombre = "Ana" }, new Cliente { Id = IdsDePrueba.Nuevo(), Nombre = "Luis" }]);

        var resultado = await _sut.ListarClientesAsync();

        Assert.Equal(2, resultado.Items.Count);
        Assert.Null(resultado.Mensaje);
    }

    /// <summary> CU-15, listar sin clientes: lista vacía con mensaje (no es error). </summary>
    [Fact]
    public async Task ListarClientesAsync_WithNoClientes_ReturnsEmptyListWithMessage()
    {
        _clienteRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var resultado = await _sut.ListarClientesAsync();

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay clientes registrados", resultado.Mensaje);
    }

    /// <summary> CU-15, alta: una fecha de nacimiento futura se rechaza y no se crea el cliente. </summary>
    [Fact]
    public async Task CrearClienteAsync_WithFutureBirthDate_ThrowsFechaNacimientoInvalidaException()
    {
        await Assert.ThrowsAsync<FechaNacimientoInvalidaException>(() =>
            _sut.CrearClienteAsync(new ClienteCreateDTO { Nombre = "Futuro", Documento = "41000333", Email = "f@test.com", FechaNacimiento = new DateTime(2100, 1, 1) }));
        _clienteRepo.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary> CU-15, modificación: una fecha de nacimiento de 1900 se rechaza y no se guarda. </summary>
    [Fact]
    public async Task ActualizarClienteAsync_WithInvalidBirthDate_ThrowsFechaNacimientoInvalidaException()
    {
        var cliente = new Cliente { Id = IdsDePrueba.Nuevo(), Nombre = "Ana", Email = "ana@test.com", Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<FechaNacimientoInvalidaException>(() =>
            _sut.ActualizarClienteAsync(cliente.Id, new AdminClienteUpdateDTO { FechaNacimiento = new DateTime(1900, 1, 1) }));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }
}
