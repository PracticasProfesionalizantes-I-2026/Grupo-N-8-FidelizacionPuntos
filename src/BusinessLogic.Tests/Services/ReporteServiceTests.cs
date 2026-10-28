using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests de los reportes del admin (CU-24). </summary>
public class ReporteServiceTests
{
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaRepo = new();
    private readonly ReporteService _sut;

    /// <summary> Arma el service con los repositorios mockeados. </summary>
    public ReporteServiceTests() => _sut = new ReporteService(_movimientoRepo.Object, _auditoriaRepo.Object);

    /// <summary> Pide un reporte de octubre de 2026. </summary>
    private static ReporteQueryDTO Octubre(TipoReporte tipo) => new()
    {
        Tipo = tipo,
        PeriodoDesde = new DateTime(2026, 10, 1),
        PeriodoHasta = new DateTime(2026, 10, 31)
    };

    /// <summary> CU-24: los totales salen de los movimientos del período (RN-19) e incluyen el ranking. </summary>
    [Fact]
    public async Task GenerarReporteAsync_WithValidParams_ReturnsConsistentReport()
    {
        var beneficio = new Beneficio { Id = Guid.NewGuid(), Nombre = "Café" };
        var cliente = Guid.NewGuid();
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([
            new Movimiento { ClienteId = cliente, Tipo = TipoMovimiento.Acumulacion, Puntos = 300 },
            new Movimiento { ClienteId = cliente, Tipo = TipoMovimiento.Canje, Puntos = -100, BeneficioId = beneficio.Id, Beneficio = beneficio },
            new Movimiento { ClienteId = cliente, Tipo = TipoMovimiento.Canje, Puntos = -100, BeneficioId = beneficio.Id, Beneficio = beneficio },
            new Movimiento { ClienteId = cliente, Tipo = TipoMovimiento.Vencimiento, Puntos = -20 }
        ]);
        _auditoriaRepo.Setup(r => r.ContarAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(4);

        var reporte = await _sut.GenerarReporteAsync(Octubre(TipoReporte.BeneficiosMasCanjeados));

        Assert.Equal(300, reporte.PuntosAcumulados);
        Assert.Equal(200, reporte.PuntosCanjeados);
        Assert.Equal(20, reporte.PuntosVencidos);
        Assert.Equal(1, reporte.ClientesConMovimientos);
        Assert.Equal(4, reporte.RegistrosAuditoria);
        var primero = Assert.Single(reporte.Ranking);
        Assert.Equal(2, primero.CantidadCanjes);
        Assert.Null(reporte.Mensaje);
    }

    /// <summary> CU-24, flujo 4a: un período sin movimientos devuelve el reporte vacío con mensaje. </summary>
    [Fact]
    public async Task GenerarReporteAsync_WithNoDataInPeriod_ReturnsEmptyReport()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([]);

        var reporte = await _sut.GenerarReporteAsync(Octubre(TipoReporte.Resumen));

        Assert.Equal(0, reporte.CantidadMovimientos);
        Assert.Equal("No hay datos disponibles para el período seleccionado", reporte.Mensaje);
    }

    /// <summary> CU-24: si periodoHasta viene sin hora, el reporte toma ese día completo. </summary>
    [Fact]
    public async Task GenerarReporteAsync_WithDateOnlyEnd_IncludesWholeLastDay()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([]);

        var reporte = await _sut.GenerarReporteAsync(Octubre(TipoReporte.Resumen));

        Assert.Equal(new DateTime(2026, 10, 31, 23, 59, 59), reporte.PeriodoHasta, TimeSpan.FromSeconds(1));
    }
}
