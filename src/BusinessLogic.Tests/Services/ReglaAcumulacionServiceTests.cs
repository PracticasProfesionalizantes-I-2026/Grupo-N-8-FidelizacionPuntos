using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests de ReglaAcumulacionService (CU-19). El repositorio es mock: nunca se toca la base. </summary>
public class ReglaAcumulacionServiceTests
{
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly ReglaAcumulacionService _sut;

    /// <summary> Arma el service bajo prueba con el repositorio mockeado. </summary>
    public ReglaAcumulacionServiceTests()
    {
        _sut = new ReglaAcumulacionService(_reglaRepo.Object);
    }

    /// <summary>
    /// Fecha de hoy para armar fechas relativas: con fechas fijas (ej. 2026-12-01) los tests de alta
    /// empezarían a fallar solos cuando esa fecha pase, porque no se puede crear una regla con fecha pasada.
    /// </summary>
    private static readonly DateTime Hoy = DateTime.UtcNow.Date;

    /// <summary> Crea una regla válida de ejemplo, para no repetir el armado en cada test. </summary>
    private static ReglaAcumulacion NuevaRegla(DateTime vigenciaDesde) => new()
    {
        Id = IdsDePrueba.Nuevo(),
        PuntosPorMonto = 0.1m,
        VigenciaDesde = vigenciaDesde,
        DiasVigenciaPuntos = 180,
        FechaCreacion = DateTime.UtcNow.AddDays(-10)
    };

    /// <summary> CU-19, listar: devuelve todas las versiones, la más nueva primero. </summary>
    [Fact]
    public async Task ObtenerReglasAcumulacionAsync_ReturnsAllRules()
    {
        var vieja = NuevaRegla(new DateTime(2026, 1, 1));
        var nueva = NuevaRegla(new DateTime(2026, 6, 1));
        _reglaRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([vieja, nueva]);

        var resultado = await _sut.ObtenerReglasAcumulacionAsync();

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(nueva.Id, resultado.Items[0].Id);
        Assert.Null(resultado.Mensaje);
    }

    /// <summary> CU-19, listar sin reglas: lista vacía con mensaje (no es error). </summary>
    [Fact]
    public async Task ObtenerReglasAcumulacionAsync_WithNoRules_ReturnsEmptyListWithMessage()
    {
        _reglaRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var resultado = await _sut.ObtenerReglasAcumulacionAsync();

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay reglas de acumulación configuradas", resultado.Mensaje);
    }

    /// <summary> CU-19, detalle: un id existente devuelve la regla. </summary>
    [Fact]
    public async Task ObtenerReglaAcumulacionPorIdAsync_WithExistingId_ReturnsRule()
    {
        var regla = NuevaRegla(new DateTime(2026, 1, 1));
        _reglaRepo.Setup(r => r.GetByIdAsync(regla.Id)).ReturnsAsync(regla);

        var resultado = await _sut.ObtenerReglaAcumulacionPorIdAsync(regla.Id);

        Assert.Equal(regla.Id, resultado.Id);
        Assert.Equal(0.1m, resultado.PuntosPorMonto);
    }

    /// <summary> CU-19, flujo 3c: un id inexistente lanza ReglaAcumulacionNotFoundException. </summary>
    [Fact]
    public async Task ObtenerReglaAcumulacionPorIdAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException()
    {
        _reglaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNotFoundException>(() =>
            _sut.ObtenerReglaAcumulacionPorIdAsync(IdsDePrueba.Nuevo()));
    }

    /// <summary> CU-19, alta: datos válidos crean la regla con fecha de alta registrada (RN-13). </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WithValidData_CreatesAndLogsAudit()
    {
        _reglaRepo.Setup(r => r.GetByVigenciaDesdeAsync(It.IsAny<DateTime>())).ReturnsAsync((ReglaAcumulacion?)null);
        _reglaRepo.Setup(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>())).ReturnsAsync((ReglaAcumulacion r) => r);

        var resultado = await _sut.CrearReglaAcumulacionAsync(new ReglaAcumulacionCreateDTO
        {
            PuntosPorMonto = 0.2m,
            VigenciaDesde = Hoy.AddDays(30).AddHours(15).AddMinutes(30),
            DiasVigenciaPuntos = 90
        });

        Assert.Equal(0.2m, resultado.PuntosPorMonto);
        Assert.Equal(Hoy.AddDays(30), resultado.VigenciaDesde); // Se guarda solo la fecha, sin la hora.
        Assert.NotEqual(default, resultado.FechaCreacion);                 // RN-13: quedó registrada la fecha de alta.
    }

    /// <summary> CU-19, flujo 3a: una fecha de inicio ya usada por otra versión lanza excepción y no crea nada (RN-18). </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException()
    {
        var existente = NuevaRegla(Hoy.AddDays(30));
        _reglaRepo.Setup(r => r.GetByVigenciaDesdeAsync(existente.VigenciaDesde)).ReturnsAsync(existente);

        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.CrearReglaAcumulacionAsync(new ReglaAcumulacionCreateDTO
            {
                PuntosPorMonto = 0.2m,
                VigenciaDesde = existente.VigenciaDesde,
                DiasVigenciaPuntos = 90
            }));

        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19, flujo 3a: valores no positivos lanzan excepción antes de consultar la base. </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WithNonPositiveValues_ThrowsReglaAcumulacionInvalidaException()
    {
        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.CrearReglaAcumulacionAsync(new ReglaAcumulacionCreateDTO
            {
                PuntosPorMonto = 0,
                VigenciaDesde = Hoy.AddDays(30),
                DiasVigenciaPuntos = 90
            }));

        _reglaRepo.Verify(r => r.GetByVigenciaDesdeAsync(It.IsAny<DateTime>()), Times.Never);
        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19, modificación parcial: cambia solo lo enviado y registra la fecha de modificación (RN-13). </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WithValidData_UpdatesAndLogsAudit()
    {
        var regla = NuevaRegla(Hoy.AddDays(10)); // todavía no vigente: se puede modificar
        _reglaRepo.Setup(r => r.GetByIdAsync(regla.Id)).ReturnsAsync(regla);

        var resultado = await _sut.ActualizarReglaAcumulacionAsync(regla.Id, new ReglaAcumulacionUpdateDTO { PuntosPorMonto = 0.5m });

        Assert.Equal(0.5m, resultado.PuntosPorMonto);
        Assert.Equal(180, resultado.DiasVigenciaPuntos); // No vino en el DTO: queda igual.
        Assert.NotNull(resultado.FechaModificacion);     // RN-13
        _reglaRepo.Verify(r => r.UpdateAsync(regla), Times.Once);
    }

    /// <summary> CU-19, modificación: mover la fecha de inicio a la de otra versión lanza excepción y no guarda (RN-18). </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WhenOverlappingOtherRule_ThrowsReglaAcumulacionInvalidaException()
    {
        var regla = NuevaRegla(Hoy.AddDays(10));
        var otra = NuevaRegla(Hoy.AddDays(20));
        _reglaRepo.Setup(r => r.GetByIdAsync(regla.Id)).ReturnsAsync(regla);
        _reglaRepo.Setup(r => r.GetByVigenciaDesdeAsync(otra.VigenciaDesde)).ReturnsAsync(otra);

        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.ActualizarReglaAcumulacionAsync(regla.Id, new ReglaAcumulacionUpdateDTO { VigenciaDesde = otra.VigenciaDesde }));

        _reglaRepo.Verify(r => r.UpdateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19, modificación: un id inexistente lanza ReglaAcumulacionNotFoundException. </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException()
    {
        _reglaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNotFoundException>(() =>
            _sut.ActualizarReglaAcumulacionAsync(IdsDePrueba.Nuevo(), new ReglaAcumulacionUpdateDTO { PuntosPorMonto = 0.5m }));
    }

    /// <summary> Arma un pedido de alta válido, cambiando solo lo que cada test necesita. </summary>
    private static ReglaAcumulacionCreateDTO Alta(decimal puntosPorMonto = 0.2m, int dias = 90, DateTime? vigenciaDesde = null) => new()
    {
        PuntosPorMonto = puntosPorMonto,
        VigenciaDesde = vigenciaDesde ?? Hoy.AddDays(30),
        DiasVigenciaPuntos = dias
    };

    /// <summary>
    /// CU-19, 3a: puntos por monto con más de 2 decimales (0.123, el caso de Bruno) o por encima de
    /// 99.99 se rechazan y no se crea nada.
    /// </summary>
    [Theory]
    [InlineData("0.123")]
    [InlineData("100")]
    public async Task CrearReglaAcumulacionAsync_WithPuntosPorMontoOutOfFormat_ThrowsReglaAcumulacionInvalidaException(string puntos)
    {
        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.CrearReglaAcumulacionAsync(Alta(puntosPorMonto: decimal.Parse(puntos, System.Globalization.CultureInfo.InvariantCulture))));
        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19, 3a: días de vigencia fuera de 30-730 (el caso de Bruno: 1) se rechazan. </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(29)]
    [InlineData(731)]
    public async Task CrearReglaAcumulacionAsync_WithDiasVigenciaOutOfRange_ThrowsReglaAcumulacionInvalidaException(int dias)
    {
        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() => _sut.CrearReglaAcumulacionAsync(Alta(dias: dias)));
    }

    /// <summary> CU-19, casos borde válidos: 30 y 730 días, 99.99 puntos por peso y una regla que empieza hoy. </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WithBoundaryValues_CreatesRule()
    {
        _reglaRepo.Setup(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>())).ReturnsAsync((ReglaAcumulacion r) => r);

        await _sut.CrearReglaAcumulacionAsync(Alta(dias: 30, vigenciaDesde: Hoy.AddDays(1)));
        await _sut.CrearReglaAcumulacionAsync(Alta(puntosPorMonto: 99.99m, dias: 730, vigenciaDesde: Hoy.AddDays(2)));
        await _sut.CrearReglaAcumulacionAsync(Alta(vigenciaDesde: Hoy));

        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Exactly(3));
    }

    /// <summary> CU-19: una fecha de inicio pasada (el caso de Bruno: 2025-01-01) se rechaza. </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WithPastStartDate_ThrowsReglaAcumulacionInvalidaException()
    {
        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.CrearReglaAcumulacionAsync(Alta(vigenciaDesde: new DateTime(2025, 1, 1))));
        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary>
    /// CU-19: una regla que ya entró en vigencia no se modifica (409), porque cambiaría con qué regla
    /// se calcularon compras ya registradas; no se guarda nada.
    /// </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WhenRuleAlreadyActive_ThrowsReglaAcumulacionVigenteException()
    {
        var vigente = NuevaRegla(Hoy.AddDays(-30));
        _reglaRepo.Setup(r => r.GetByIdAsync(vigente.Id)).ReturnsAsync(vigente);

        await Assert.ThrowsAsync<ReglaAcumulacionVigenteException>(() =>
            _sut.ActualizarReglaAcumulacionAsync(vigente.Id, new ReglaAcumulacionUpdateDTO { PuntosPorMonto = 0.5m }));
        _reglaRepo.Verify(r => r.UpdateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19: al modificar una regla futura, no se le puede poner una fecha de inicio pasada. </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WithPastStartDate_ThrowsReglaAcumulacionInvalidaException()
    {
        var futura = NuevaRegla(Hoy.AddDays(10));
        _reglaRepo.Setup(r => r.GetByIdAsync(futura.Id)).ReturnsAsync(futura);

        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() =>
            _sut.ActualizarReglaAcumulacionAsync(futura.Id, new ReglaAcumulacionUpdateDTO { VigenciaDesde = new DateTime(2025, 1, 1) }));
    }
}
