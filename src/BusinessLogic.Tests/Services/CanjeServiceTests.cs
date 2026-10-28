using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>
/// Tests de CanjeService (CU-07). Los repositorios son mocks; el FIFO usa el
/// <see cref="LotePuntosService"/> real, porque es un cálculo puro sin base de datos.
/// </summary>
public class CanjeServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IBeneficioRepository> _beneficioRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly CanjeService _sut;

    private readonly Guid _clienteId = Guid.NewGuid();
    private readonly Beneficio _beneficio = new() { Id = Guid.NewGuid(), Nombre = "Café gratis", CostoPuntos = 120, Activo = true };

    // Dos lotes disponibles: el más viejo con 100 y el más nuevo con 50 (saldo 150).
    private readonly Movimiento _loteViejo = new() { Fecha = DateTime.UtcNow.AddDays(-20), Tipo = TipoMovimiento.Acumulacion, PuntosDisponibles = 100 };
    private readonly Movimiento _loteNuevo = new() { Fecha = DateTime.UtcNow.AddDays(-5), Tipo = TipoMovimiento.Acumulacion, PuntosDisponibles = 50 };

    /// <summary> Arma el service y deja configurado el camino feliz: cliente, beneficio activo y saldo 150. </summary>
    public CanjeServiceTests()
    {
        _sut = new CanjeService(_clienteRepo.Object, _beneficioRepo.Object, _movimientoRepo.Object, new LotePuntosService());

        _clienteRepo.Setup(r => r.GetByIdAsync(_clienteId)).ReturnsAsync(new Cliente { Id = _clienteId });
        _beneficioRepo.Setup(r => r.GetByIdAsync(_beneficio.Id)).ReturnsAsync(_beneficio);
        _movimientoRepo.Setup(r => r.GetLotesParaCanjeAsync(_clienteId, It.IsAny<DateTime>())).ReturnsAsync([_loteViejo, _loteNuevo]);
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);
    }

    /// <summary> Pedido de canje del beneficio de prueba. </summary>
    private CanjeCreateDTO Pedido(Guid? beneficioId = null) => new() { BeneficioId = beneficioId ?? _beneficio.Id };

    /// <summary> CU-07, flujo principal: canjea 120 de 150 descontando por FIFO (RN-09): el lote viejo queda en 0 y el nuevo en 30. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithSufficientBalance_CreatesCanjeAndUpdatesSaldo()
    {
        var resultado = await _sut.CrearCanjeAsync(_clienteId, Pedido());

        Assert.Equal(120, resultado.PuntosDescontados);
        Assert.Equal(30, resultado.SaldoRestante);
        Assert.Equal("Pendiente", resultado.Estado);
        Assert.Equal(0, _loteViejo.PuntosDisponibles);  // FIFO: primero se vacía el más antiguo...
        Assert.Equal(30, _loteNuevo.PuntosDisponibles); // ...y el resto sale del siguiente.
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.Tipo == TipoMovimiento.Canje && m.Puntos == -120)), Times.Once);
    }

    /// <summary> CU-07, flujo 3a: un beneficio inexistente lanza BeneficioNotFoundException. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithNonExistentBeneficio_ThrowsBeneficioNotFoundException()
    {
        _beneficioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Beneficio?)null);

        await Assert.ThrowsAsync<BeneficioNotFoundException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido(Guid.NewGuid())));
    }

    /// <summary> CU-07, flujo 3b: un beneficio inactivo (RN-17) lanza excepción y no descuenta nada. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException()
    {
        _beneficio.Activo = false;

        await Assert.ThrowsAsync<BeneficioInactivoException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-07, flujo 3c: con saldo menor al costo (RN-08) lanza excepción y los lotes quedan intactos. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException()
    {
        _beneficio.CostoPuntos = 500;

        await Assert.ThrowsAsync<SaldoInsuficienteException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
        Assert.Equal(100, _loteViejo.PuntosDisponibles);
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-07, flujo 5a: si falla el guardado, se traduce a PersistenceException. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WhenRepositoryFails_ThrowsPersistenceException()
    {
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>()))
            .ThrowsAsync(new InvalidOperationException("Falla de conexión simulada"));

        await Assert.ThrowsAsync<PersistenceException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
    }

    /// <summary> Pedido de canje presencial del cliente de prueba (documento 40333444). </summary>
    private CanjePresencialCreateDTO PedidoPresencial() => new() { ClienteDocumento = "40333444", BeneficioId = _beneficio.Id };

    /// <summary> Configura el cliente que el empleado busca por documento en el canje presencial. </summary>
    private void ConfigurarClientePorDocumento() =>
        _clienteRepo.Setup(r => r.GetByDocumentoAsync("40333444")).ReturnsAsync(new Cliente { Id = _clienteId, Documento = "40333444" });

    /// <summary> CU-11: el canje presencial queda "Entregado" y registra qué empleado lo hizo (RN-13). </summary>
    [Fact]
    public async Task CrearCanjePresencialAsync_WithSufficientBalance_CreatesCanjeAndLogsAudit()
    {
        ConfigurarClientePorDocumento();
        var empleadoId = Guid.NewGuid();

        var resultado = await _sut.CrearCanjePresencialAsync(PedidoPresencial(), empleadoId);

        Assert.Equal("Entregado", resultado.Estado);
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m =>
            m.EmpleadoId == empleadoId && m.EstadoCanje == EstadoCanje.Entregado)), Times.Once);
    }

    /// <summary> CU-11, flujo 1a: un documento desconocido lanza ClienteNotFoundException. </summary>
    [Fact]
    public async Task CrearCanjePresencialAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.CrearCanjePresencialAsync(PedidoPresencial(), Guid.NewGuid()));
    }

    /// <summary> CU-11, flujo 3a: un beneficio inactivo (RN-17) lanza BeneficioInactivoException. </summary>
    [Fact]
    public async Task CrearCanjePresencialAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException()
    {
        ConfigurarClientePorDocumento();
        _beneficio.Activo = false;

        await Assert.ThrowsAsync<BeneficioInactivoException>(() => _sut.CrearCanjePresencialAsync(PedidoPresencial(), Guid.NewGuid()));
    }

    /// <summary> CU-11, flujo 3b: con saldo menor al costo (RN-08) lanza SaldoInsuficienteException. </summary>
    [Fact]
    public async Task CrearCanjePresencialAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException()
    {
        ConfigurarClientePorDocumento();
        _beneficio.CostoPuntos = 500;

        await Assert.ThrowsAsync<SaldoInsuficienteException>(() => _sut.CrearCanjePresencialAsync(PedidoPresencial(), Guid.NewGuid()));
    }

    /// <summary> CU-11, flujo 5a: si falla el guardado, se traduce a PersistenceException. </summary>
    [Fact]
    public async Task CrearCanjePresencialAsync_WhenRepositoryFails_ThrowsPersistenceException()
    {
        ConfigurarClientePorDocumento();
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ThrowsAsync(new InvalidOperationException("Falla simulada"));

        await Assert.ThrowsAsync<PersistenceException>(() => _sut.CrearCanjePresencialAsync(PedidoPresencial(), Guid.NewGuid()));
    }

    /// <summary> RN-14: un cliente dado de baja no puede canjear. </summary>
    [Fact]
    public async Task CrearCanjeAsync_WithInactiveCliente_ThrowsCuentaInactivaException()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(_clienteId)).ReturnsAsync(new Cliente { Id = _clienteId, Activo = false });

        await Assert.ThrowsAsync<CuentaInactivaException>(() => _sut.CrearCanjeAsync(_clienteId, Pedido()));
    }

    /// <summary> RN-10: un canje pendiente pasa a "Entregado" y guarda qué empleado lo entregó. </summary>
    [Fact]
    public async Task MarcarEntregadoAsync_WithPendingCanje_MarksAsDelivered()
    {
        var canje = new Movimiento { Id = Guid.NewGuid(), Tipo = TipoMovimiento.Canje, Puntos = -100, EstadoCanje = EstadoCanje.Pendiente };
        _movimientoRepo.Setup(r => r.GetByIdAsync(canje.Id)).ReturnsAsync(canje);
        var empleadoId = Guid.NewGuid();

        var resultado = await _sut.MarcarEntregadoAsync(canje.Id, empleadoId);

        Assert.Equal("Entregado", resultado.Estado);
        Assert.Equal(empleadoId, canje.EmpleadoId);
        _movimientoRepo.Verify(r => r.UpdateAsync(canje), Times.Once);
    }

    /// <summary> RN-10: un canje ya entregado no se entrega de nuevo. </summary>
    [Fact]
    public async Task MarcarEntregadoAsync_WhenAlreadyDelivered_ThrowsCanjeYaEntregadoException()
    {
        var canje = new Movimiento { Id = Guid.NewGuid(), Tipo = TipoMovimiento.Canje, EstadoCanje = EstadoCanje.Entregado };
        _movimientoRepo.Setup(r => r.GetByIdAsync(canje.Id)).ReturnsAsync(canje);

        await Assert.ThrowsAsync<CanjeYaEntregadoException>(() => _sut.MarcarEntregadoAsync(canje.Id, Guid.NewGuid()));
        _movimientoRepo.Verify(r => r.UpdateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> RN-10: un id que no es un canje (por ejemplo, una acumulación) lanza CanjeNotFoundException. </summary>
    [Fact]
    public async Task MarcarEntregadoAsync_WithNonCanjeMovimiento_ThrowsCanjeNotFoundException()
    {
        var acumulacion = new Movimiento { Id = Guid.NewGuid(), Tipo = TipoMovimiento.Acumulacion };
        _movimientoRepo.Setup(r => r.GetByIdAsync(acumulacion.Id)).ReturnsAsync(acumulacion);

        await Assert.ThrowsAsync<CanjeNotFoundException>(() => _sut.MarcarEntregadoAsync(acumulacion.Id, Guid.NewGuid()));
    }
}
