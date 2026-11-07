using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="BeneficioService"/> (CU-17). El repositorio es un mock: nunca se toca la base.</summary>
public class BeneficioServiceTests
{
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly BeneficioService _sut;

    /// <summary>Arma el service bajo prueba con el repositorio mockeado.</summary>
    public BeneficioServiceTests()
    {
        _sut = new BeneficioService(_beneficioRepo.Object);
    }

    /// <summary>CU-17, alta: datos válidos crean el beneficio activo.</summary>
    [Fact]
    public async Task CrearBeneficioAsync_WithValidData_CreatesAndReturnsBeneficio()
    {
        _beneficioRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Beneficio?)null);
        _beneficioRepo.Setup(r => r.CreateAsync(It.IsAny<Beneficio>())).ReturnsAsync((Beneficio b) => b);

        var resultado = await _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Descuento 10%", CostoPuntos = 100 });

        Assert.Equal("Descuento 10%", resultado.Nombre);
        Assert.True(resultado.Activo);
    }

    /// <summary>CU-17, flujo 3a: costo cero o negativo lanza <see cref="CostoInvalidoException"/> (RN-16).</summary>
    [Fact]
    public async Task CrearBeneficioAsync_WithNonPositiveCosto_ThrowsCostoInvalidoException()
    {
        await Assert.ThrowsAsync<CostoInvalidoException>(() =>
            _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Gratis", CostoPuntos = 0 }));
    }

    /// <summary>CU-17, flujo 3a: un costo de más de 6 dígitos enteros lanza <see cref="CostoInvalidoException"/> (RN-16).</summary>
    [Fact]
    public async Task CrearBeneficioAsync_WithCostoAboveMaximum_ThrowsCostoInvalidoException()
    {
        await Assert.ThrowsAsync<CostoInvalidoException>(() =>
            _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Muy Caro", CostoPuntos = 1_000_000 }));

        _beneficioRepo.Verify(r => r.CreateAsync(It.IsAny<Beneficio>()), Times.Never);
    }

    /// <summary>CU-17, flujo 3a, caso borde: el costo máximo 999.999 sí se acepta (RN-16).</summary>
    [Fact]
    public async Task CrearBeneficioAsync_WithMaximumCosto_CreatesBeneficio()
    {
        _beneficioRepo.Setup(r => r.GetByNombreAsync(It.IsAny<string>())).ReturnsAsync((Beneficio?)null);
        _beneficioRepo.Setup(r => r.CreateAsync(It.IsAny<Beneficio>())).ReturnsAsync((Beneficio b) => b);

        var resultado = await _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Tope", CostoPuntos = 999_999 });

        Assert.Equal(999_999, resultado.CostoPuntos);
    }

    /// <summary>CU-17, flujo 3b: nombre repetido lanza <see cref="BeneficioDuplicadoException"/> (RN-23).</summary>
    [Fact]
    public async Task CrearBeneficioAsync_WhenNombreExists_ThrowsBeneficioDuplicadoException()
    {
        _beneficioRepo.Setup(r => r.GetByNombreAsync("Descuento 10%")).ReturnsAsync(new Beneficio { Nombre = "Descuento 10%" });

        await Assert.ThrowsAsync<BeneficioDuplicadoException>(() =>
            _sut.CrearAsync(new BeneficioCreateDTO { Nombre = "Descuento 10%", CostoPuntos = 100 }));
    }

    /// <summary>CU-17, modificación: solo cambian los campos informados en el DTO.</summary>
    [Fact]
    public async Task ActualizarBeneficioAsync_WithValidData_UpdatesAndReturnsBeneficio()
    {
        var beneficio = new Beneficio { Id = IdsDePrueba.Nuevo(), Nombre = "Descuento 10%", CostoPuntos = 100, Activo = true };
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);

        var resultado = await _sut.ActualizarAsync(beneficio.Id, new BeneficioUpdateDTO { CostoPuntos = 150 });

        Assert.Equal(150, resultado.CostoPuntos);
        Assert.Equal("Descuento 10%", resultado.Nombre);
    }

    /// <summary>CU-17, flujo 3a en la modificación: un costo mayor a 999.999 no se guarda (RN-16).</summary>
    [Fact]
    public async Task ActualizarBeneficioAsync_WithCostoAboveMaximum_ThrowsCostoInvalidoException()
    {
        var beneficio = new Beneficio { Id = IdsDePrueba.Nuevo(), Nombre = "Descuento 10%", CostoPuntos = 100, Activo = true };
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);

        await Assert.ThrowsAsync<CostoInvalidoException>(() =>
            _sut.ActualizarAsync(beneficio.Id, new BeneficioUpdateDTO { CostoPuntos = 1_000_000 }));

        _beneficioRepo.Verify(r => r.UpdateAsync(It.IsAny<Beneficio>()), Times.Never);
    }

    /// <summary>CU-17, flujo 3c: modificar un Id inexistente lanza <see cref="BeneficioNotFoundException"/>.</summary>
    [Fact]
    public async Task ActualizarBeneficioAsync_WithNonExistentId_ThrowsBeneficioNotFoundException()
    {
        _beneficioRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Beneficio?)null);

        await Assert.ThrowsAsync<BeneficioNotFoundException>(() =>
            _sut.ActualizarAsync(IdsDePrueba.Nuevo(), new BeneficioUpdateDTO { CostoPuntos = 150 }));
    }

    /// <summary>CU-17, desactivación: baja lógica (RN-17), el beneficio queda con `Activo = false`.</summary>
    [Fact]
    public async Task DesactivarBeneficioAsync_WithExistingId_DeactivatesBeneficio()
    {
        var beneficio = new Beneficio { Id = IdsDePrueba.Nuevo(), Nombre = "Descuento 10%", CostoPuntos = 100, Activo = true };
        _beneficioRepo.Setup(r => r.GetByIdAsync(beneficio.Id)).ReturnsAsync(beneficio);

        var resultado = await _sut.DesactivarAsync(beneficio.Id);

        Assert.False(resultado.Activo);
        _beneficioRepo.Verify(r => r.UpdateAsync(beneficio), Times.Once);
    }

    /// <summary>CU-17: el listado del admin incluye los beneficios inactivos y los ordena por nombre.</summary>
    [Fact]
    public async Task ListarTodosAsync_ReturnsActiveAndInactiveOrderedByName()
    {
        _beneficioRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new Beneficio { Nombre = "Zapatillas", CostoPuntos = 900, Activo = true },
            new Beneficio { Nombre = "Café", CostoPuntos = 50, Activo = false }
        ]);

        var resultado = await _sut.ListarTodosAsync();

        Assert.Equal(["Café", "Zapatillas"], resultado.Items.Select(b => b.Nombre));
        Assert.False(resultado.Items[0].Activo);
    }

    /// <summary>CU-17: sin beneficios cargados devuelve una lista vacía con mensaje (no es un error).</summary>
    [Fact]
    public async Task ListarTodosAsync_WithNoBeneficios_ReturnsEmptyListWithMessage()
    {
        _beneficioRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var resultado = await _sut.ListarTodosAsync();

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay beneficios registrados", resultado.Mensaje);
    }

    /// <summary>CU-17: las categorías se listan numeradas desde 1, con su nombre legible.</summary>
    [Fact]
    public void ListarCategorias_ReturnsCategoriesStartingAtOneWithReadableNames()
    {
        var resultado = _sut.ListarCategorias();

        Assert.Equal([1, 2, 3, 4], resultado.Items.Select(c => c.Id));
        Assert.Equal(["Descuento", "Producto gratis", "Experiencia", "Otro"], resultado.Items.Select(c => c.Nombre));
    }
}
