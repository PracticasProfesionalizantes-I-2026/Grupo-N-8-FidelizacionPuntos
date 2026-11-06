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

    /// <summary>
    /// CU-24: el período (hora Argentina) se consulta en UTC, igual que los movimientos (CU-20), así el
    /// reporte y la consulta de movimientos cuentan los mismos días.
    /// </summary>
    [Fact]
    public async Task GenerarReporteAsync_QueriesPeriodInUtcFromArgentineTime()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([]);

        await _sut.GenerarReporteAsync(Octubre(TipoReporte.Resumen));

        var desdeUtc = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
        var hastaUtc = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc).AddTicks(-1);
        _movimientoRepo.Verify(r => r.BuscarAsync(null, null, desdeUtc, hastaUtc), Times.Once);
        _auditoriaRepo.Verify(r => r.ContarAsync(desdeUtc, hastaUtc), Times.Once);
    }

    /// <summary>
    /// Comparación sin indicador ("misma fecha, distinto movimiento"): una serie por indicador, con un
    /// punto por día (incluidos los días en 0). Ventas y Canjes cuentan movimientos; PuntosOtorgados suma
    /// acumulaciones y bonos de cumpleaños.
    /// </summary>
    [Fact]
    public async Task CompararAsync_WithoutIndicador_ReturnsOneSeriesPerIndicador()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([
            new Movimiento { Tipo = TipoMovimiento.Acumulacion, Puntos = 100, Fecha = new DateTime(2026, 11, 1, 15, 0, 0) },
            new Movimiento { Tipo = TipoMovimiento.Acumulacion, Puntos = 50, Fecha = new DateTime(2026, 11, 1, 16, 0, 0) },
            new Movimiento { Tipo = TipoMovimiento.BonoCumpleanos, Puntos = 200, Fecha = new DateTime(2026, 11, 2, 12, 0, 0) },
            new Movimiento { Tipo = TipoMovimiento.Canje, Puntos = -100, Fecha = new DateTime(2026, 11, 3, 12, 0, 0) }
        ]);

        var resultado = await _sut.CompararAsync(new ComparacionQueryDTO { Periodo1Desde = new DateTime(2026, 11, 1), Periodo1Hasta = new DateTime(2026, 11, 3) });

        Assert.Equal(["Ventas", "Canjes", "PuntosOtorgados"], resultado.Series.Select(s => s.Indicador));
        var ventas = resultado.Series[0];
        Assert.Equal([2, 0, 0], ventas.Valores.Select(v => v.Valor));
        Assert.Equal(new DateOnly(2026, 11, 1), ventas.Valores[0].Fecha);
        Assert.Equal(1, resultado.Series[1].Total);
        Assert.Equal(350, resultado.Series[2].Total);
        Assert.Null(resultado.Mensaje);
    }

    /// <summary>
    /// Comparación con indicador y dos períodos ("fechas distintas"): una serie por período, y el
    /// repositorio se consulta una vez por cada período.
    /// </summary>
    [Fact]
    public async Task CompararAsync_WithIndicadorAndTwoPeriods_ReturnsOneSeriesPerPeriod()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([
            new Movimiento { Tipo = TipoMovimiento.Canje, Puntos = -100, Fecha = new DateTime(2026, 10, 1, 15, 0, 0) }
        ]);

        var resultado = await _sut.CompararAsync(new ComparacionQueryDTO
        {
            Indicador = IndicadorDashboard.Canjes,
            Periodo1Desde = new DateTime(2026, 10, 1), Periodo1Hasta = new DateTime(2026, 10, 7),
            Periodo2Desde = new DateTime(2026, 11, 1), Periodo2Hasta = new DateTime(2026, 11, 7)
        });

        Assert.Equal(2, resultado.Series.Count);
        Assert.All(resultado.Series, s => Assert.Equal("Canjes", s.Indicador));
        Assert.All(resultado.Series, s => Assert.Equal(7, s.Valores.Count));
        Assert.Equal(new DateTime(2026, 11, 1), resultado.Series[1].PeriodoDesde);
        _movimientoRepo.Verify(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Exactly(2));
    }

    /// <summary>
    /// Una venta a las 22:30 hora Argentina del 06/11 (guardada como 07/11 01:30 UTC) cuenta en el día
    /// 06/11 de la serie, no en el 07/11.
    /// </summary>
    [Fact]
    public async Task CompararAsync_WithSaleAfter21ArgentineTime_CountsOnArgentineDay()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([
            new Movimiento { Tipo = TipoMovimiento.Acumulacion, Puntos = 100, Fecha = new DateTime(2026, 11, 7, 1, 30, 0) }
        ]);

        var resultado = await _sut.CompararAsync(new ComparacionQueryDTO
        {
            Indicador = IndicadorDashboard.Ventas,
            Periodo1Desde = new DateTime(2026, 11, 6), Periodo1Hasta = new DateTime(2026, 11, 7)
        });

        var serie = Assert.Single(resultado.Series);
        Assert.Equal(1, serie.Valores.Single(v => v.Fecha == new DateOnly(2026, 11, 6)).Valor);
        Assert.Equal(0, serie.Valores.Single(v => v.Fecha == new DateOnly(2026, 11, 7)).Valor);
    }

    /// <summary> Comparación, flujo 4a: si todas las series dan 0, se informa que no hay datos. </summary>
    [Fact]
    public async Task CompararAsync_WithNoData_ReturnsMessage()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([]);

        var resultado = await _sut.CompararAsync(new ComparacionQueryDTO { Periodo1Desde = new DateTime(2026, 11, 1), Periodo1Hasta = new DateTime(2026, 11, 2) });

        Assert.Equal(3, resultado.Series.Count);
        Assert.Equal("No hay datos disponibles para el período seleccionado", resultado.Mensaje);
    }
}
