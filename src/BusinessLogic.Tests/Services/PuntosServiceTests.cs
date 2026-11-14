using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests de los procesos automáticos: vencimiento de puntos (CU-22) y bono de cumpleaños (CU-26). </summary>
public class PuntosServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly Mock<IReglaAcumulacionRepository> _reglaRepo = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<IUnidadDeTrabajo> _unidadDeTrabajo = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IConfiguration> _configuracion = new();
    private readonly PuntosService _sut;

    private static readonly DateTime Hoy = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    /// <summary> Arma el service: bono de 500 puntos, regla vigente de 180 días y repositorio que guarda sin fallar. </summary>
    public PuntosServiceTests()
    {
        _configuracion.Setup(c => c["Puntos:BonoCumpleanos"]).Returns("500");
        _reglaRepo.Setup(r => r.GetVigenteAsync(It.IsAny<DateTime>())).ReturnsAsync(new ReglaAcumulacion { DiasVigenciaPuntos = 180 });
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<Movimiento>())).ReturnsAsync((Movimiento m) => m);
        _sut = new PuntosService(_clienteRepo.Object, _movimientoRepo.Object, _reglaRepo.Object, _auditoria.Object,
            _unidadDeTrabajo.Object, _email.Object, _configuracion.Object, NullLogger<PuntosService>.Instance);
    }

    /// <summary> CU-22: deja los lotes vencidos en 0, registra un vencimiento negativo, lo audita y avisa al cliente. </summary>
    [Fact]
    public async Task AplicarVencimientoAsync_WithExpiredLotes_DiscountsFifoAndLogsAudit()
    {
        var clienteId = IdsDePrueba.Nuevo();
        var lote1 = new Movimiento { ClienteId = clienteId, Fecha = Hoy.AddDays(-200), PuntosDisponibles = 80 };
        var lote2 = new Movimiento { ClienteId = clienteId, Fecha = Hoy.AddDays(-190), PuntosDisponibles = 20 };
        _movimientoRepo.Setup(r => r.GetLotesVencidosAsync(Hoy)).ReturnsAsync([lote1, lote2]);
        _clienteRepo.Setup(r => r.GetByIdAsync(clienteId)).ReturnsAsync(new Cliente { Id = clienteId, Email = "c@test.com" });

        var resultado = await _sut.AplicarVencimientoAsync(Hoy);

        Assert.Equal("Exito", resultado.Estado);
        Assert.Equal(100, resultado.PuntosAfectados);
        Assert.Equal(0, lote1.PuntosDisponibles);
        Assert.Equal(0, lote2.PuntosDisponibles);
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.Tipo == TipoMovimiento.Vencimiento && m.Puntos == -100)), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("Proceso.VencimientoPuntos", null, "Sistema", It.IsAny<int?>(), It.IsAny<string>()), Times.Once);
        _email.Verify(e => e.EnviarAsync("c@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary> CU-22, flujo 2a: sin lotes vencidos no cambia nada. </summary>
    [Fact]
    public async Task AplicarVencimientoAsync_WithNoExpiredLotes_MakesNoChanges()
    {
        _movimientoRepo.Setup(r => r.GetLotesVencidosAsync(Hoy)).ReturnsAsync([]);

        var resultado = await _sut.AplicarVencimientoAsync(Hoy);

        Assert.Equal("SinCambios", resultado.Estado);
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary>
    /// CU-22, flujo 4a: si falla un cliente, se descartan sus cambios pendientes (CRIT-02: no se arrastran
    /// al guardado del siguiente) y se sigue con el resto (Error parcial).
    /// </summary>
    [Fact]
    public async Task AplicarVencimientoAsync_WhenRepositoryFailsForOneCliente_ContinuesWithRemainingClientes()
    {
        var clienteQueFalla = IdsDePrueba.Nuevo();
        var clienteOk = IdsDePrueba.Nuevo();
        var loteQueFalla = new Movimiento { ClienteId = clienteQueFalla, Fecha = Hoy.AddDays(-200), PuntosDisponibles = 30 };
        var loteOk = new Movimiento { ClienteId = clienteOk, Fecha = Hoy.AddDays(-200), PuntosDisponibles = 40 };
        _movimientoRepo.Setup(r => r.GetLotesVencidosAsync(Hoy)).ReturnsAsync([loteQueFalla, loteOk]);
        _movimientoRepo.Setup(r => r.CreateAsync(It.Is<Movimiento>(m => m.ClienteId == clienteQueFalla)))
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));

        var resultado = await _sut.AplicarVencimientoAsync(Hoy);

        Assert.Equal("ErrorParcial", resultado.Estado);
        Assert.Equal(1, resultado.ClientesProcesados);
        Assert.Equal(1, resultado.ClientesConError);
        Assert.Equal(0, loteOk.PuntosDisponibles);
        _unidadDeTrabajo.Verify(u => u.DescartarCambios(), Times.Once);
    }

    /// <summary>
    /// CRIT-02: si el vencimiento se guardó pero la auditoría falla, NO se revierten los lotes en memoria
    /// (eso hacía que el SaveChanges siguiente devolviera los puntos vencidos): se descarta solo lo
    /// pendiente, el cliente cuenta como procesado y se sigue con el siguiente.
    /// </summary>
    [Fact]
    public async Task AplicarVencimientoAsync_WhenAuditFailsAfterSaving_KeepsExpirationAndContinues()
    {
        var clienteA = IdsDePrueba.Nuevo();
        var clienteB = IdsDePrueba.Nuevo();
        var loteA = new Movimiento { ClienteId = clienteA, Fecha = Hoy.AddDays(-200), PuntosDisponibles = 70 };
        var loteB = new Movimiento { ClienteId = clienteB, Fecha = Hoy.AddDays(-190), PuntosDisponibles = 20 };
        _movimientoRepo.Setup(r => r.GetLotesVencidosAsync(Hoy)).ReturnsAsync([loteA, loteB]);
        _auditoria.Setup(a => a.RegistrarAsync("Proceso.VencimientoPuntos", null, "Sistema", It.IsAny<int?>(),
                It.Is<string>(d => d.StartsWith($"Cliente {clienteA}:"))))
            .ThrowsAsync(new InvalidOperationException("Falla simulada de auditoría"));

        var resultado = await _sut.AplicarVencimientoAsync(Hoy);

        Assert.Equal(2, resultado.ClientesProcesados);
        Assert.Equal(90, resultado.PuntosAfectados);
        Assert.Equal(0, loteA.PuntosDisponibles); // No se "restauró" a 70.
        Assert.Equal(0, loteB.PuntosDisponibles);
        _unidadDeTrabajo.Verify(u => u.DescartarCambios(), Times.Once);
    }

    /// <summary> CU-26: acredita el bono a quien cumple años hoy, como lote con vencimiento, y lo audita. </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WithMatchingClientes_AccruesPointsAndLogsAudit()
    {
        var cumpleanero = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true, FechaNacimiento = new DateTime(1990, 10, 28) };
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(10, 28)).ReturnsAsync([cumpleanero]);

        var resultado = await _sut.AplicarBonoCumpleanosAsync(Hoy);

        Assert.Equal("Exito", resultado.Estado);
        Assert.Equal(1, resultado.ClientesProcesados);
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m =>
            m.ClienteId == cumpleanero.Id && m.Tipo == TipoMovimiento.BonoCumpleanos
            && m.Puntos == 500 && m.FechaVencimiento == Hoy.AddDays(180))), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync("Proceso.BonoCumpleanos", null, "Sistema", It.IsAny<int?>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary> CU-26, flujo 2a: si ya recibió el bono este año, se omite sin error (RN-29). </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WhenAlreadyGrantedThisYear_SkipsCliente()
    {
        var cumpleanero = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true, FechaNacimiento = new DateTime(1990, 10, 28) };
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(10, 28)).ReturnsAsync([cumpleanero]);
        _movimientoRepo.Setup(r => r.ExisteBonoEnAnioAsync(cumpleanero.Id, 2026)).ReturnsAsync(true);

        var resultado = await _sut.AplicarBonoCumpleanosAsync(Hoy);

        Assert.Equal("SinCambios", resultado.Estado);
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<Movimiento>()), Times.Never);
    }

    /// <summary> CU-26, flujo 4a: si falla un cliente, se sigue con el siguiente (queda para el reintento). </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WhenRepositoryFailsForOneCliente_ContinuesWithNext()
    {
        var falla = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true, FechaNacimiento = new DateTime(1990, 10, 28) };
        var ok = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true, FechaNacimiento = new DateTime(1995, 10, 28) };
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(10, 28)).ReturnsAsync([falla, ok]);
        _movimientoRepo.Setup(r => r.CreateAsync(It.Is<Movimiento>(m => m.ClienteId == falla.Id)))
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));

        var resultado = await _sut.AplicarBonoCumpleanosAsync(Hoy);

        Assert.Equal("ErrorParcial", resultado.Estado);
        Assert.Equal(1, resultado.ClientesProcesados);
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<Movimiento>(m => m.ClienteId == ok.Id)), Times.Once);
        _unidadDeTrabajo.Verify(u => u.DescartarCambios(), Times.Once);
    }

    /// <summary> CU-26: los nacidos un 29/2 reciben el bono el 28/2 en los años no bisiestos. </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_WithLeapDayBirthday_GrantsOnFeb28InNonLeapYear()
    {
        var bisiesto = new Cliente { Id = IdsDePrueba.Nuevo(), Activo = true, FechaNacimiento = new DateTime(2000, 2, 29) };
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(2, 28)).ReturnsAsync([]);
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(2, 29)).ReturnsAsync([bisiesto]);

        var resultado = await _sut.AplicarBonoCumpleanosAsync(new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, resultado.ClientesProcesados);
    }

    /// <summary> En un año bisiesto el 28/2 es un día normal: no se busca a los nacidos el 29/2. </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_OnFeb28OfLeapYear_DoesNotIncludeLeapDayBirthdays()
    {
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(2, 28)).ReturnsAsync([]);

        var resultado = await _sut.AplicarBonoCumpleanosAsync(new DateTime(2028, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("SinCambios", resultado.Estado);
        _clienteRepo.Verify(r => r.GetActivosNacidosElAsync(2, 29), Times.Never);
    }

    /// <summary> CRIT-03: el proceso ya no carga la tabla completa de clientes en memoria. </summary>
    [Fact]
    public async Task AplicarBonoCumpleanosAsync_DoesNotLoadAllClientes()
    {
        _clienteRepo.Setup(r => r.GetActivosNacidosElAsync(10, 28)).ReturnsAsync([]);

        await _sut.AplicarBonoCumpleanosAsync(Hoy);

        _clienteRepo.Verify(r => r.GetAllAsync(), Times.Never);
    }
}
