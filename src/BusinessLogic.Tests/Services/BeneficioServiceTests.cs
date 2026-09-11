using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="BeneficioService"/> (CU-06, CU-17).</summary>
public class BeneficioServiceTests
{
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly BeneficioService _sut;

    public BeneficioServiceTests()
    {
        _sut = new BeneficioService(_beneficioRepo.Object, _auditoriaService.Object);
    }

    [Fact]
    public async Task ConsultarActivosAsync_ReturnsOnlyActiveBenefits()
    {
        _beneficioRepo.Setup(r => r.GetActivosAsync()).ReturnsAsync([new Beneficio { Nombre = "Descuento", Activo = true }]);

        var resultado = await _sut.ConsultarActivosAsync();

        Assert.Single(resultado);
    }

    [Fact]
    public async Task CrearAsync_WithValidData_CreatesAndReturnsBeneficio()
    {
        _beneficioRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Beneficio?)null);
        _beneficioRepo.Setup(r => r.CreateAsync(It.IsAny<Beneficio>())).ReturnsAsync((Beneficio b) => b);

        var resultado = await _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Descuento 10%", CostoPuntos = 100 });

        Assert.Equal(100, resultado.CostoPuntos);
    }

    [Fact]
    public async Task CrearAsync_WithInvalidCosto_ThrowsCostoInvalidoException()
    {
        await Assert.ThrowsAsync<CostoInvalidoException>(() =>
            _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Descuento", CostoPuntos = 0 }));
    }

    [Fact]
    public async Task CrearAsync_WhenNombreExists_ThrowsBeneficioDuplicadoException()
    {
        _beneficioRepo.Setup(r => r.GetByNombreAsync("Descuento")).ReturnsAsync(new Beneficio { Nombre = "Descuento" });

        await Assert.ThrowsAsync<BeneficioDuplicadoException>(() =>
            _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Descuento", CostoPuntos = 100 }));
    }

    [Fact]
    public async Task DesactivarAsync_WithExistingBeneficio_SetsActivoFalse()
    {
        var beneficio = new Beneficio { Id = Guid.NewGuid(), Nombre = "Descuento", Activo = true };
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);

        var resultado = await _sut.DesactivarAsync(beneficio.Id);

        Assert.False(resultado.Activo);
    }
}
