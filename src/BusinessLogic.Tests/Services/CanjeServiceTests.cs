using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>
/// Tests de CanjeService (CU-07). Los repositorios son mocks; el FIFO usa el
/// <see cref="LotePuntosService"/> real, porque es un cálculo puro sin base de datos.
/// </summary>
public class CanjeServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly CanjeService _sut;

    private readonly Guid _clienteId = Guid.NewGuid();
    private readonly Beneficio _beneficio = new() { Id = Guid.NewGuid(), Nombre = "Café gratis", CostoPuntos = 120, Activo = true };

    // Dos lotes disponibles: el más viejo con 100 y el más nuevo con 50 (saldo 150).
    private readonly Movimiento _loteViejo = new() { Fecha = DateTime.UtcNow.AddDays(-20), Tipo = TipoMovimiento.Acumulacion, PuntosDisponibles = 100 };
    private readonly Movimiento _loteNuevo = new() { Fecha = DateTime.UtcNow.AddDays(-5), Tipo = TipoMovimiento.Acumulacion, PuntosDisponibles = 50 };

    /// <summary> Arma el service y deja configurado el camino feliz: cliente, beneficio activo y saldo 150. </summary>
    public CanjeServiceTests()
    {
        _sut = new CanjeService(_clienteRepo.Object, _beneficioRepo.Object, _movimientoRepo.Object, new LotePuntosService());

        _clienteRepo.Setup(r => r.GetByIdAsync(_clienteId)).ReturnsAsync(new Cliente { Id = _clienteId });
        _beneficioRepo.Setup(r => r.GetByIdAsync(_beneficio.Id)).ReturnsAsync(_beneficio);
        _movimientoRepo.Setup(r => r.GetLotesParaCanjeAsync(_clienteId, It.IsAny<DateTime>())).ReturnsAsync([_loteViejo, _loteNuevo]);
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);
    }

    /// <summary> Pedido de canje del beneficio de prueba. </summary>
    private CanjeCreateDTO Pedido(Guid? beneficioId = null) => new() { BeneficioId = beneficioId ?? _beneficio.Id };

    /// <summary> CU-07, flujo principal: canjea 120 de 150 descontando por FIFO (RN-09): el lote viejo queda en 0 y el nuevo en 30. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithSufficientBalance_CreatesCanjeAndUpdatesSaldo()
    {
        var resultado = await _sut.CrearCanjeAsync(_clienteId, Pedido());

        Assert.Equal(120, resultado.PuntosDescontados);
        Assert.Equal(30, resultado.SaldoRestante);
        Assert.Equal("Pendiente", resultado.Estado);
        Assert.Equal(0, _loteViejo.PuntosDisponibles);  // FIFO: primero se vacía el más antiguo...
        Assert.Equal(30, _loteNuevo.PuntosDisponibles); // ...y el resto sale del siguiente.
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.Tipo == TipoMovimiento.Canje && m.Puntos == -120)), Times.Once);
    }

    /// <summary> CU-07, flujo 3a: un beneficio inexistente lanza BeneficioNotFoundException. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithNonExistentBeneficio_ThrowsBeneficioNotFoundException()
    {
        _beneficioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Beneficio?)null);

        await Assert.ThrowsAsync<BeneficioNotFoundException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido(Guid.NewGuid())));
    }

    /// <summary> CU-07, flujo 3b: un beneficio inactivo (RN-17) lanza excepción y no descuenta nada. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException()
    {
        _beneficio.Activo = false;

        await Assert.ThrowsAsync<BeneficioInactivoException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-07, flujo 3c: con saldo menor al costo (RN-08) lanza excepción y los lotes quedan intactos. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException()
    {
        _beneficio.CostoPuntos = 500;

        await Assert.ThrowsAsync<SaldoInsuficienteException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
        Assert.Equal(100, _loteViejo.PuntosDisponibles);
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-07, flujo 5a: si falla el guardado, se traduce a PersistenceException. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WhenRepositoryFails_ThrowsPersistenceException()
    {
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>()))
            .ThrowsAsync(new InvalidOperationException("Falla de conexión simulada"));

        await Assert.ThrowsAsync<PersistenceException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
    }
}
