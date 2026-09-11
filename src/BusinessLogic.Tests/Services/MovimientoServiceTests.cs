using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>Tests de <see cref="MovimientoService"/> (CU-05, CU-10, CU-20).</summary>
public class MovimientoServiceTests
{
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly Mock<IAuditoriaService> _auditoriaService = new();
    private readonly MovimientoService _sut;

    public MovimientoServiceTests()
    {
        _sut = new MovimientoService(_movimientoRepo.Object, _clienteRepo.Object, _reglaRepo.Object, _auditoriaService.Object);
    }

    private static MovimientoAcumulacionCreateDTO DtoValido() => new()
    {
        ClienteDocumento = "40111222",
        Items = [new ItemCompraDTO { Producto = "Gaseosa", Cantidad = 1, Monto = 1000m }]
    };

    [Fact]
    public async Task RegistrarAcumulacionAsync_WithValidData_AccruesPointsAndLogsAudit()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Documento = "40111222", Activo = true };
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(cliente.Documento)).ReturnsAsync(cliente);
        _reglaRepo.Setup(r => r.GetActivaAsync()).ReturnsAsync(new ReglaAcumulacion { PuntosPorMonto = 1m, DiasVigenciaPuntos = 365 });
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);

        var resultado = await _sut.RegistrarAcumulacionAsync(Guid.NewGuid(), DtoValido());

        Assert.Equal(1000, resultado.Puntos); // 1000 * 1 punto por unidad de moneda
    }

    [Fact]
    public async Task RegistrarAcumulacionAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.RegistrarAcumulacionAsync(Guid.NewGuid(), DtoValido()));
    }

    [Fact]
    public async Task RegistrarAcumulacionAsync_WithInactiveCliente_ThrowsCuentaInactivaException()
    {
        var cliente = new Cliente { Id = Guid.NewGuid(), Documento = "40111222", Activo = false };
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(cliente.Documento)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<CuentaInactivaException>(() => _sut.RegistrarAcumulacionAsync(Guid.NewGuid(), DtoValido()));
    }

    [Fact]
    public async Task RegistrarAcumulacionAsync_WithInvalidItem_ThrowsValidationException()
    {
        var dto = new MovimientoAcumulacionCreateDTO
        {
            ClienteDocumento = "40111222",
            Items = [new ItemCompraDTO { Producto = "Gaseosa", Cantidad = 0, Monto = 1000m }]
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.RegistrarAcumulacionAsync(Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task ConsultarHistorialClienteAsync_ReturnsMappedMovimientos()
    {
        var clienteId = Guid.NewGuid();
        _movimientoRepo.Setup(r => r.GetByClienteIdAsync(clienteId)).ReturnsAsync([new Movimiento { Id = Guid.NewGuid(), ClienteId = clienteId }]);

        var resultado = await _sut.ConsultarHistorialClienteAsync(clienteId);

        Assert.Single(resultado);
    }
}
