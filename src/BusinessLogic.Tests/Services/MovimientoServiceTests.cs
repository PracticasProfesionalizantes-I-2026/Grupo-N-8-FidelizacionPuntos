using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests de MovimientoService (CU-10). Los repositorios son mocks: nunca se toca la base. </summary>
public class MovimientoServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IProductoRepository> _productoRepo = new();
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly MovimientoService _sut;

    private readonly Cliente _cliente = new() { Id = Guid.NewGuid(), Documento = "40333444", Activo = true, Categoria = CategoriaCliente.Estandar };
    private readonly Producto _producto = new() { Id = Guid.NewGuid(), Nombre = "Hamburguesa", Precio = 1000, Activo = true };

    /// <summary>
    /// Arma el service y deja configurado el "camino feliz": cliente y producto existentes,
    /// regla vigente de 0.1 pts/$ y 180 días, y un repositorio que guarda sin fallar.
    /// Cada test cambia solo lo que necesita para su caso.
    /// </summary>
    public MovimientoServiceTests()
    {
        _sut = new MovimientoService(_clienteRepo.Object, _productoRepo.Object, _reglaRepo.Object, _movimientoRepo.Object);

        _clienteRepo.Setup(r => r.GetByDocumentoAsync(_cliente.Documento)).ReturnsAsync(_cliente);
        _productoRepo.Setup(r => r.GetByIdAsync(_producto.Id)).ReturnsAsync(_producto);
        _reglaRepo.Setup(r => r.GetVigenteAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new ReglaAcumulacion { PuntosPorMonto = 0.1m, DiasVigenciaPuntos = 180 });
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);
        _movimientoRepo.Setup(r => r.GetLotesVigentesPorClienteAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new Movimiento { PuntosDisponibles = 150 }]);
    }

    /// <summary> Compra de 2 unidades del producto de prueba ($1000 c/u = $2000). </summary>
    private AcumulacionCreateDTO Compra(string documento = "40333444") => new()
    {
        ClienteDocumento = documento,
        Items = [new ItemCompraDTO { ProductoId = _producto.Id, Cantidad = 2 }]
    };

    /// <summary> CU-10, flujo principal: $2000 x 0.1 = 200 puntos, guardados como lote pendiente 15 días (N2) y auditado (RN-13). </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithValidPurchase_AccruesPointsPerRulesAndLogsAudit()
    {
        var resultado = await _sut.RegistrarAcumulacionAsync(Compra());

        Assert.Equal(2000, resultado.MontoTotal);
        Assert.Equal(200, resultado.PuntosAcreditados);
        Assert.Equal(150, resultado.SaldoDisponible); // Los 200 nuevos están pendientes: no suman todavía (N2).
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m =>
            m.Tipo == TipoMovimiento.Acumulacion
            && m.Puntos == 200
            && m.FechaDisponible > DateTime.UtcNow.AddDays(14))), Times.Once);
    }

    /// <summary> CU-10, N1: un cliente Gold acumula x1.5 ($2000 x 0.1 x 1.5 = 300). </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithGoldCliente_AppliesMultiplier()
    {
        _cliente.Categoria = CategoriaCliente.Gold;

        var resultado = await _sut.RegistrarAcumulacionAsync(Compra());

        Assert.Equal(1.5m, resultado.Multiplicador);
        Assert.Equal(300, resultado.PuntosAcreditados);
    }

    /// <summary> CU-10, flujo 3a: un documento que no es de ningún cliente lanza ClienteNotFoundException. </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync("99999999")).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.RegistrarAcumulacionAsync(Compra("99999999")));
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-10, flujo 3b: un documento con letras se rechaza antes de buscar al cliente. </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithMalformedDocumento_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<DocumentoInvalidoException>(() => _sut.RegistrarAcumulacionAsync(Compra("abc123")));
        _clienteRepo.Verify(r => r.GetByDocumentoAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary> CU-10, flujo 2a: un producto inactivo (RN-26) rechaza la compra y no acredita nada. </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithInactiveProducto_ThrowsItemCompraInvalidoException()
    {
        _producto.Activo = false;

        await Assert.ThrowsAsync<ItemCompraInvalidoException>(() => _sut.RegistrarAcumulacionAsync(Compra()));
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-10, precondición: sin regla vigente (CU-19) no se puede calcular y no se acredita nada. </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WithoutVigenteRule_ThrowsReglaAcumulacionNoVigenteException()
    {
        _reglaRepo.Setup(r => r.GetVigenteAsync(It.IsAny<DateTime>())).ReturnsAsync((ReglaAcumulacion?)null);

        await Assert.ThrowsAsync<ReglaAcumulacionNoVigenteException>(() => _sut.RegistrarAcumulacionAsync(Compra()));
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-10, flujo 4a: si el repositorio falla al guardar, se traduce a PersistenceException. </summary>
    [Fact]
    public async Task RegistrarAcumulacionAsync_WhenRepositoryFails_ThrowsPersistenceException()
    {
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>()))
            .ThrowsAsync(new InvalidOperationException("Falla de conexión simulada"));

        await Assert.ThrowsAsync<PersistenceException>(() => _sut.RegistrarAcumulacionAsync(Compra()));
    }
}
