using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="ReglaAcumulacionService"/> (CU-19).</summary>
public class ReglaAcumulacionServiceTests
{
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly ReglaAcumulacionService _sut;

    public ReglaAcumulacionServiceTests()
    {
        _sut = new ReglaAcumulacionService(_reglaRepo.Object, _auditoriaService.Object);
    }

    [Fact]
    public async Task ObtenerTodasAsync_ReturnsAllRules()
    {
        _reglaRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([new ReglaAcumulacion { Id = Guid.NewGuid() }]);

        var resultado = await _sut.ObtenerTodasAsync();

        Assert.Single(resultado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException()
    {
        _reglaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNotFoundException>(() => _sut.ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CrearAsync_WithValidData_CreatesAndLogsAudit()
    {
        _reglaRepo.Setup(r => r.GetActivasSolapadasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime?>(), null)).ReturnsAsync([]);
        _reglaRepo.Setup(r => r.CreateAsync(It.IsAny<ReglaAcumulacion>())).ReturnsAsync((ReglaAcumulacion r) => r);

        var dto = new ReglaAcumulacionCreateDTO { PuntosPorMonto = 1m, VigenciaDesde = DateTime.UtcNow, DiasVigenciaPuntos = 365 };
        var resultado = await _sut.CrearAsync(dto);

        Assert.Equal(365, resultado.DiasVigenciaPuntos);
        _auditoriaService.Verify(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<Shared.Enums.ActorTipo>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException()
    {
        _reglaRepo.Setup(r => r.GetActivasSolapadasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime?>(), null))
            .ReturnsAsync([new ReglaAcumulacion { Id = Guid.NewGuid() }]);

        var dto = new ReglaAcumulacionCreateDTO { PuntosPorMonto = 1m, VigenciaDesde = DateTime.UtcNow, DiasVigenciaPuntos = 365 };

        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() => _sut.CrearAsync(dto));
    }

    [Fact]
    public async Task CrearAsync_WithInvalidDiasVigencia_ThrowsReglaAcumulacionInvalidaException()
    {
        var dto = new ReglaAcumulacionCreateDTO { PuntosPorMonto = 1m, VigenciaDesde = DateTime.UtcNow, DiasVigenciaPuntos = 0 };

        await Assert.ThrowsAsync<ReglaAcumulacionInvalidaException>(() => _sut.CrearAsync(dto));
    }
}
