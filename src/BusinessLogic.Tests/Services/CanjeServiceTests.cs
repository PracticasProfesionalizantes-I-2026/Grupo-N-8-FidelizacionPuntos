using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="CanjeService"/> (CU-07, CU-08, CU-11, CU-13).</summary>
public class CanjeServiceTests
{
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IPuntosService> _puntosService = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly CanjeService _sut;

    public CanjeServiceTests()
    {
        _sut = new CanjeService(_movimientoRepo.Object, _beneficioRepo.Object, _clienteRepo.Object, _puntosService.Object, _auditoriaService.Object);
    }

    [Fact]
    public async Task CrearCanjeAsync_WithValidData_DescuentaPuntosAndCreatesCanje()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true };
        var beneficio = new Beneficio { Id = Guid.NewGuid(), Nombre = "Descuento", CostoPuntos = 100, Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);

        var resultado = await _sut.CrearCanjeAsync(cliente.Id, new CanjeCreateDTO { BeneficioId = beneficio.Id });

        Assert.Equal(100, resultado.PuntosUtilizados);
        _puntosService.Verify(p => p.DescontarPuntosFifoAsync(cliente.Id, 100), Times.Once);
    }

    [Fact]
    public async Task CrearCanjeAsync_WithNonExistentBeneficio_ThrowsBeneficioNotFoundException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);
        _beneficioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Beneficio?)null);

        await Assert.ThrowsAsync<BeneficioNotFoundException>(() =>
            _sut.CrearCanjeAsync(cliente.Id, new CanjeCreateDTO { BeneficioId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task CrearCanjeAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true };
        var beneficio = new Beneficio { Id = Guid.NewGuid(), Activo = false };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);

        await Assert.ThrowsAsync<BeneficioInactivoException>(() =>
            _sut.CrearCanjeAsync(cliente.Id, new CanjeCreateDTO { BeneficioId = beneficio.Id }));
    }

    [Fact]
    public async Task CrearCanjeAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true };
        var beneficio = new Beneficio { Id = Guid.NewGuid(), CostoPuntos = 500, Activo = true };
        _clienteRepo.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);
        _puntosService.Setup(p => p.DescontarPuntosFifoAsync(cliente.Id, beneficio.CostoPuntos))
            .ThrowsAsync(new SaldoInsuficienteException("insuficiente"));

        await Assert.ThrowsAsync<SaldoInsuficienteException>(() =>
            _sut.CrearCanjeAsync(cliente.Id, new CanjeCreateDTO { BeneficioId = beneficio.Id }));
    }

    [Fact]
    public async Task CrearCanjePresencialAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() =>
            _sut.CrearCanjePresencialAsync(Guid.NewGuid(), new CanjeCreateDTO { BeneficioId = Guid.NewGuid(), ClienteDocumento = "40111222" }));
    }

    [Fact]
    public async Task ConsultarPorDocumentoAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.ConsultarPorDocumentoAsync("40111222"));
    }
}
