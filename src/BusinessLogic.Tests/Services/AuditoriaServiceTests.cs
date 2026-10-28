using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests del registro (RN-13) y la consulta (CU-21) de la auditoría. </summary>
public class AuditoriaServiceTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaRepo = new();
    private readonly AuditoriaService _sut;

    /// <summary> Arma el service con el repositorio mockeado. </summary>
    public AuditoriaServiceTests() => _sut = new AuditoriaService(_auditoriaRepo.Object);

    /// <summary> CU-21: con criterios devuelve los registros que coinciden. </summary>
    [Fact]
    public async Task ConsultarAuditoriaAsync_WithFilters_ReturnsMatchingRecords()
    {
        _auditoriaRepo.Setup(r => r.BuscarAsync(null, null, "Canjes", null))
            .ReturnsAsync([new Auditoria { TipoOperacion = "Canjes.CrearCanje", ActorRol = "Cliente" }]);

        var resultado = await _sut.ConsultarAuditoriaAsync(new AuditoriaFiltroDTO { TipoOperacion = "Canjes" });

        Assert.Equal("Canjes.CrearCanje", Assert.Single(resultado.Items).TipoOperacion);
    }

    /// <summary> CU-21, flujo 3a: sin ningún criterio no consulta la base y devuelve una lista vacía. </summary>
    [Fact]
    public async Task ConsultarAuditoriaAsync_WithNoFilters_ReturnsEmptyList()
    {
        var resultado = await _sut.ConsultarAuditoriaAsync(new AuditoriaFiltroDTO());

        Assert.Empty(resultado.Items);
        Assert.NotNull(resultado.Mensaje);
        _auditoriaRepo.Verify(r => r.BuscarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    /// <summary> CU-21, flujo 4a: con criterios pero sin coincidencias devuelve una lista vacía con mensaje. </summary>
    [Fact]
    public async Task ConsultarAuditoriaAsync_WithNoMatches_ReturnsEmptyList()
    {
        _auditoriaRepo.Setup(r => r.BuscarAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid?>())).ReturnsAsync([]);

        var resultado = await _sut.ConsultarAuditoriaAsync(new AuditoriaFiltroDTO { ActorId = Guid.NewGuid() });

        Assert.Empty(resultado.Items);
        Assert.Equal("No existen resultados para los filtros seleccionados", resultado.Mensaje);
    }

    /// <summary> RN-13: registrar crea un registro con los datos de la operación. </summary>
    [Fact]
    public async Task RegistrarAsync_CreatesRecord()
    {
        var actorId = Guid.NewGuid();

        await _sut.RegistrarAsync("Canjes.CrearCanje", actorId, "Cliente", null, "POST /api/canjes -> 201");

        _auditoriaRepo.Verify(r => r.CreateAsync(It.Is<Auditoria>(a => a.ActorId == actorId && a.TipoOperacion == "Canjes.CrearCanje")), Times.Once);
    }

    /// <summary> CU-21: el detalle de un id inexistente lanza AuditoriaNotFoundException. </summary>
    [Fact]
    public async Task ObtenerPorIdAsync_WithUnknownId_ThrowsAuditoriaNotFoundException()
    {
        _auditoriaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auditoria?)null);

        await Assert.ThrowsAsync<AuditoriaNotFoundException>(() => _sut.ObtenerPorIdAsync(Guid.NewGuid()));
    }
}
