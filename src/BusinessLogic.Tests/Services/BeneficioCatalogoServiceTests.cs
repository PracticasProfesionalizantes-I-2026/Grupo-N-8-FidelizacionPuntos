using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests del catálogo de beneficios del cliente (CU-06) en <see cref="BeneficioService"/>. </summary>
public class BeneficioCatalogoServiceTests
{
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly BeneficioService _sut;

    /// <summary> Arma el service con el repositorio mockeado. </summary>
    public BeneficioCatalogoServiceTests() => _sut = new BeneficioService(_beneficioRepo.Object);

    /// <summary> CU-06: solo devuelve los beneficios activos (RN-07). </summary>
    [Fact]
    public async Task ListarBeneficiosAsync_ReturnsOnlyActiveBeneficios()
    {
        _beneficioRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new Beneficio { Nombre = "Activo", CostoPuntos = 100, Activo = true },
            new Beneficio { Nombre = "Inactivo", CostoPuntos = 50, Activo = false }
        ]);

        var resultado = await _sut.ListarBeneficiosAsync();

        Assert.Equal("Activo", Assert.Single(resultado.Items).Nombre);
    }

    /// <summary> CU-06, flujo 4a: sin beneficios activos devuelve una lista vacía con mensaje. </summary>
    [Fact]
    public async Task ListarBeneficiosAsync_WithNoActiveBeneficios_ReturnsEmptyList()
    {
        _beneficioRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([new Beneficio { Nombre = "Inactivo", Activo = false }]);

        var resultado = await _sut.ListarBeneficiosAsync();

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay beneficios disponibles", resultado.Mensaje);
    }
}
