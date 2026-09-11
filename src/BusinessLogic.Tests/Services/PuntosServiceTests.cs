using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="PuntosService"/> (CU-04, CU-22, CU-23, CU-26).</summary>
public class PuntosServiceTests
{
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly PuntosService _sut;

    public PuntosServiceTests()
    {
        _sut = new PuntosService(
            _movimientoRepo.Object, _clienteRepo.Object, _reglaRepo.Object,
            _auditoriaService.Object, Mock.Of<ILogger<PuntosService>>());
    }

    private static Movimiento Lote(Guid clienteId, int puntos, DateTime fecha) => new()
    {
        Id = Guid.NewGuid(),
        ClienteId = clienteId,
        Tipo = TipoMovimiento.Acumulacion,
        Puntos = puntos,
        PuntosDisponibles = puntos,
        Fecha = fecha
    };

    [Fact]
    public async Task ConsultarSaldoAsync_SumsPuntosDisponiblesOfLotesVigentes()
    {
        var clienteId = Guid.NewGuid();
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(clienteId, It.IsAny<DateTime>()))
            .ReturnsAsync([Lote(clienteId, 50, DateTime.UtcNow), Lote(clienteId, 30, DateTime.UtcNow)]);

        var resultado = await _sut.ConsultarSaldoAsync(clienteId);

        Assert.Equal(80, resultado.PuntosDisponibles);
    }

    [Fact]
    public async Task DescontarPuntosFifoAsync_ConsumesOldestLoteFirst()
    {
        var clienteId = Guid.NewGuid();
        var loteViejo = Lote(clienteId, 30, DateTime.UtcNow.AddDays(-10));
        var loteNuevo = Lote(clienteId, 50, DateTime.UtcNow);
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(clienteId, It.IsAny<DateTime>()))
            .ReturnsAsync([loteViejo, loteNuevo]); // ya ordenados por fecha asc, como devuelve el repositorio real

        await _sut.DescontarPuntosFifoAsync(clienteId, 40);

        Assert.Equal(0, loteViejo.PuntosDisponibles); // se consume completo primero (RN-09, FIFO)
        Assert.Equal(40, loteNuevo.PuntosDisponibles); // se consumen solo los 10 puntos restantes
    }

    [Fact]
    public async Task DescontarPuntosFifoAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException()
    {
        var clienteId = Guid.NewGuid();
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(clienteId, It.IsAny<DateTime>()))
            .ReturnsAsync([Lote(clienteId, 10, DateTime.UtcNow)]);

        await Assert.ThrowsAsync<SaldoInsuficienteException>(() => _sut.DescontarPuntosFifoAsync(clienteId, 50));
    }

    [Fact]
    public async Task AplicarVencimientoAsync_WithExpiredLotes_ZerosBalanceAndLogsMovimiento()
    {
        var lote = Lote(Guid.NewGuid(), 25, DateTime.UtcNow.AddDays(-400));
        _movimientoRepo.Setup(r => r.GetLotesVencidosConSaldoAsync(It.IsAny<DateTime>())).ReturnsAsync([lote]);

        await _sut.AplicarVencimientoAsync();

        Assert.Equal(0, lote.PuntosDisponibles);
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.Tipo == TipoMovimiento.Vencimiento && m.Puntos == -25)), Times.Once);
    }

    [Fact]
    public async Task AplicarVencimientoAsync_WithNoExpiredLotes_DoesNothing()
    {
        _movimientoRepo.Setup(r => r.GetLotesVencidosConSaldoAsync(It.IsAny<DateTime>())).ReturnsAsync([]);

        await _sut.AplicarVencimientoAsync();

        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WithMatchingClientes_AccruesPointsAndLogsAudit()
    {
        var hoy = DateTime.UtcNow;
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true, FechaNacimiento = new DateTime(1990, hoy.Month, hoy.Day) };
        _clienteRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([cliente]);
        _movimientoRepo.Setup(r => r.ExisteBonoCumpleanosEnAnioAsync(cliente.Id, hoy.Year)).ReturnsAsync(false);
        _reglaRepo.Setup(r => r.GetActivaAsync()).ReturnsAsync(new ReglaAcumulacion { DiasVigenciaPuntos = 365 });

        await _sut.AplicarBonoCumpleanosAsync();

        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.Tipo == TipoMovimiento.BonoCumpleanos && m.ClienteId == cliente.Id)), Times.Once);
    }

    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WhenAlreadyGrantedThisYear_SkipsCliente()
    {
        var hoy = DateTime.UtcNow;
        var cliente = new Cliente { Id = Guid.NewGuid(), Activo = true, FechaNacimiento = new DateTime(1990, hoy.Month, hoy.Day) };
        _clienteRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([cliente]);
        _movimientoRepo.Setup(r => r.ExisteBonoCumpleanosEnAnioAsync(cliente.Id, hoy.Year)).ReturnsAsync(true);

        await _sut.AplicarBonoCumpleanosAsync();

        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }
}
