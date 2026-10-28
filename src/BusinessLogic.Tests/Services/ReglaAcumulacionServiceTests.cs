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

    /// <summary> Crea una regla válida de ejemplo, para no repetir el armado en cada test. </summary>
    private static ReglaAcumulacion NuevaRegla(DateTime vigenciaDesde) => new()
    {
        Id = Guid.NewGuid(),
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

        Assert.Equal(2, resultado.Count);
        Assert.Equal(nueva.Id, resultado[0].Id);
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
        _reglaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNotFoundException>(() =>
            _sut.ObtenerReglaAcumulacionPorIdAsync(Guid.NewGuid()));
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
            VigenciaDesde = new DateTime(2026, 12, 1, 15, 30, 0),
            DiasVigenciaPuntos = 90
        });

        Assert.Equal(0.2m, resultado.PuntosPorMonto);
        Assert.Equal(new DateTime(2026, 12, 1), resultado.VigenciaDesde); // Se guarda solo la fecha, sin la hora.
        Assert.NotEqual(default, resultado.FechaCreacion);                 // RN-13: quedó registrada la fecha de alta.
    }

    /// <summary> CU-19, flujo 3a: una fecha de inicio ya usada por otra versión lanza excepción y no crea nada (RN-18). </summary>
    [Fact]
    public async Task CrearReglaAcumulacionAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException()
    {
        var existente = NuevaRegla(new DateTime(2026, 12, 1));
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
                VigenciaDesde = new DateTime(2026, 12, 1),
                DiasVigenciaPuntos = 90
            }));

        _reglaRepo.Verify(r => r.GetByVigenciaDesdeAsync(It.IsAny<DateTime>()), Times.Never);
        _reglaRepo.Verify(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>()), Times.Never);
    }

    /// <summary> CU-19, modificación parcial: cambia solo lo enviado y registra la fecha de modificación (RN-13). </summary>
    [Fact]
    public async Task ActualizarReglaAcumulacionAsync_WithValidData_UpdatesAndLogsAudit()
    {
        var regla = NuevaRegla(new DateTime(2026, 1, 1));
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
        var regla = NuevaRegla(new DateTime(2026, 1, 1));
        var otra = NuevaRegla(new DateTime(2026, 6, 1));
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
        _reglaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNotFoundException>(() =>
            _sut.ActualizarReglaAcumulacionAsync(Guid.NewGuid(), new ReglaAcumulacionUpdateDTO { PuntosPorMonto = 0.5m }));
    }
}
