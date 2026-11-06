using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary>
/// Tests de <see cref="ClienteService"/> para los CU del cliente autenticado (CU-03, CU-05, CU-08)
/// y del punto de venta (CU-12, CU-13). Los repositorios son mocks.
/// </summary>
public class ClientePerfilServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IMovimientoRepository> _movimientoRepo = new();
    private readonly ClienteService _sut;
    private readonly Cliente _cliente = new() { Id = Guid.NewGuid(), Nombre = "Ana", Documento = "40555666", Email = "ana@test.com", Activo = true };

    /// <summary> Arma el service con un cliente existente. </summary>
    public ClientePerfilServiceTests()
    {
        _sut = new ClienteService(_clienteRepo.Object, _movimientoRepo.Object);
        _clienteRepo.Setup(r => r.GetByIdAsync(_cliente.Id)).ReturnsAsync(_cliente);
    }

    /// <summary> CU-03: modifica solo lo enviado (nombre) y guarda. </summary>
    [Fact]
    public async Task ActualizarPerfilAsync_WithValidData_UpdatesAndReturnsCliente()
    {
        var resultado = await _sut.ActualizarPerfilAsync(_cliente.Id, new ClienteUpdateDTO { Nombre = "Ana María" });

        Assert.Equal("Ana María", resultado.Nombre);
        Assert.Equal("ana@test.com", resultado.Email); // No vino en el DTO: queda igual.
        _clienteRepo.Verify(r => r.UpdateAsync(_cliente), Times.Once);
    }

    /// <summary> CU-03, flujo 2b: intentar cambiar el documento lanza excepción (RN-04) y no guarda. </summary>
    [Fact]
    public async Task ActualizarPerfilAsync_WhenDocumentoChanged_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<DocumentoNoModificableException>(() =>
            _sut.ActualizarPerfilAsync(_cliente.Id, new ClienteUpdateDTO { Documento = "99999999" }));
        _clienteRepo.Verify(r => r.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary> CU-03, flujo 3a: un email de otro cliente lanza EmailDuplicadoException (RN-01). </summary>
    [Fact]
    public async Task ActualizarPerfilAsync_WhenEmailAlreadyExists_ThrowsEmailDuplicadoException()
    {
        _clienteRepo.Setup(r => r.GetByEmailAsync("otro@test.com")).ReturnsAsync(new Cliente { Id = Guid.NewGuid(), Email = "otro@test.com" });

        await Assert.ThrowsAsync<EmailDuplicadoException>(() =>
            _sut.ActualizarPerfilAsync(_cliente.Id, new ClienteUpdateDTO { Email = "otro@test.com" }));
    }

    /// <summary> CU-03, baja: la cuenta queda inactiva (baja lógica, RN-14). </summary>
    [Fact]
    public async Task DarseDeBajaAsync_WithAuthenticatedCliente_DeactivatesCliente()
    {
        await _sut.DarseDeBajaAsync(_cliente.Id);

        Assert.False(_cliente.Activo);
        _clienteRepo.Verify(r => r.UpdateAsync(_cliente), Times.Once);
    }

    /// <summary> CU-05: devuelve los movimientos en orden cronológico (RN-06) y pasa los filtros al repositorio. </summary>
    [Fact]
    public async Task ConsultarHistorialAsync_WithFilters_ReturnsOrderedMovimientos()
    {
        var nuevo = new Movimiento { Id = Guid.NewGuid(), Fecha = new DateTime(2026, 10, 2), Tipo = TipoMovimiento.Acumulacion, Puntos = 50 };
        var viejo = new Movimiento { Id = Guid.NewGuid(), Fecha = new DateTime(2026, 9, 1), Tipo = TipoMovimiento.Acumulacion, Puntos = 100 };
        _movimientoRepo.Setup(r => r.BuscarAsync(_cliente.Id, TipoMovimiento.Acumulacion, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync([nuevo, viejo]);

        var resultado = await _sut.ConsultarHistorialAsync(_cliente.Id, new HistorialFiltroDTO { TipoMovimiento = TipoMovimiento.Acumulacion });

        Assert.Equal([viejo.Id, nuevo.Id], resultado.Items.Select(m => m.Id));
        Assert.Null(resultado.Mensaje);
    }

    /// <summary>
    /// CU-05: el historial usa el mismo criterio de fechas que la consulta del admin (CU-20): un día
    /// sin hora se consulta completo en hora Argentina, convertido a UTC.
    /// </summary>
    [Fact]
    public async Task ConsultarHistorialAsync_WithSameDayRange_QueriesWholeArgentineDayInUtc()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(It.IsAny<Guid?>(), It.IsAny<TipoMovimiento?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync([]);
        var dia = new DateTime(2026, 11, 6);

        await _sut.ConsultarHistorialAsync(_cliente.Id, new HistorialFiltroDTO { FechaDesde = dia, FechaHasta = dia });

        _movimientoRepo.Verify(r => r.BuscarAsync(_cliente.Id, null,
            new DateTime(2026, 11, 6, 3, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 7, 3, 0, 0, DateTimeKind.Utc).AddTicks(-1)), Times.Once);
    }

    /// <summary> CU-05, flujo 4a: sin movimientos devuelve una lista vacía con mensaje (no es error). </summary>
    [Fact]
    public async Task ConsultarHistorialAsync_WithNoMovimientos_ReturnsEmptyList()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(It.IsAny<Guid?>(), It.IsAny<TipoMovimiento?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync([]);

        var resultado = await _sut.ConsultarHistorialAsync(_cliente.Id, new HistorialFiltroDTO());

        Assert.Empty(resultado.Items);
        Assert.Equal("No hay movimientos para mostrar", resultado.Mensaje);
    }

    /// <summary> CU-08: devuelve los canjes del cliente con su estado (RN-10). </summary>
    [Fact]
    public async Task ConsultarCanjesAsync_ReturnsCanjesWithStatus()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(_cliente.Id, TipoMovimiento.Canje, null, null)).ReturnsAsync([
            new Movimiento { Tipo = TipoMovimiento.Canje, Puntos = -100, EstadoCanje = EstadoCanje.Pendiente, Beneficio = new Beneficio { Nombre = "Café" } }
        ]);

        var resultado = await _sut.ConsultarCanjesAsync(_cliente.Id);

        var canje = Assert.Single(resultado.Items);
        Assert.Equal("Pendiente", canje.Estado);
        Assert.Equal(100, canje.PuntosDescontados);
        Assert.Equal("Café", canje.BeneficioNombre);
    }

    /// <summary> CU-08, flujo 4a: sin canjes devuelve una lista vacía con mensaje. </summary>
    [Fact]
    public async Task ConsultarCanjesAsync_WithNoCanjes_ReturnsEmptyList()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(_cliente.Id, TipoMovimiento.Canje, null, null)).ReturnsAsync([]);

        var resultado = await _sut.ConsultarCanjesAsync(_cliente.Id);

        Assert.Empty(resultado.Items);
        Assert.Equal("Aún no realizaste ningún canje", resultado.Mensaje);
    }

    /// <summary> CU-12: crea el cliente sin contraseña (la define después con CU-25). </summary>
    [Fact]
    public async Task CrearClienteDesdePOSAsync_WithValidData_CreatesAndReturnsCliente()
    {
        Cliente? creado = null;
        _clienteRepo.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => { creado = c; return c; });

        var resultado = await _sut.CrearClienteDesdePOSAsync(new ClienteCreateDTO { Nombre = "Juan", Documento = "41222333", Email = "juan@test.com" });

        Assert.Equal("41222333", resultado.Documento);
        Assert.Null(creado!.PasswordHash);
    }

    /// <summary> CU-12, flujo 3a: un documento ya registrado lanza ClienteDuplicadoException (RN-01). </summary>
    [Fact]
    public async Task CrearClienteDesdePOSAsync_WhenDocumentoExists_ThrowsClienteDuplicadoException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync("40555666")).ReturnsAsync(_cliente);

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() =>
            _sut.CrearClienteDesdePOSAsync(new ClienteCreateDTO { Nombre = "Otro", Documento = "40555666", Email = "x@test.com" }));
        _clienteRepo.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    /// <summary> CU-13: con el id del cliente devuelve sus canjes con estado. </summary>
    [Fact]
    public async Task ConsultarCanjesDeClienteAsync_WithExistingId_ReturnsCanjesWithStatus()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(_cliente.Id, TipoMovimiento.Canje, null, null)).ReturnsAsync([
            new Movimiento { Tipo = TipoMovimiento.Canje, Puntos = -50, EstadoCanje = EstadoCanje.Entregado }
        ]);

        var resultado = await _sut.ConsultarCanjesDeClienteAsync(_cliente.Id);

        Assert.Equal("Entregado", Assert.Single(resultado.Items).Estado);
    }

    /// <summary> CU-13, flujo 3a: un id que no es de ningún cliente lanza ClienteNotFoundException. </summary>
    [Fact]
    public async Task ConsultarCanjesDeClienteAsync_WithUnknownId_ThrowsClienteNotFoundException()
    {
        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.ConsultarCanjesDeClienteAsync(Guid.NewGuid()));
    }

    /// <summary> CU-13, flujo 4a: un cliente sin canjes devuelve una lista vacía con mensaje. </summary>
    [Fact]
    public async Task ConsultarCanjesDeClienteAsync_WithNoCanjes_ReturnsEmptyList()
    {
        _movimientoRepo.Setup(r => r.BuscarAsync(_cliente.Id, TipoMovimiento.Canje, null, null)).ReturnsAsync([]);

        var resultado = await _sut.ConsultarCanjesDeClienteAsync(_cliente.Id);

        Assert.Empty(resultado.Items);
        Assert.Equal("El cliente aún no registra beneficios canjeados", resultado.Mensaje);
    }

    /// <summary>
    /// Búsqueda por DNI del punto de venta: devuelve el id del cliente con datos mínimos (nombre,
    /// documento y categoría), sin email ni teléfono.
    /// </summary>
    [Fact]
    public async Task BuscarPorDocumentoAsync_WithExistingDocumento_ReturnsReducedCliente()
    {
        _cliente.Categoria = CategoriaCliente.Gold;
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(_cliente.Documento)).ReturnsAsync(_cliente);

        var resultado = await _sut.BuscarPorDocumentoAsync(new ClienteBusquedaDTO { Documento = _cliente.Documento });

        Assert.Equal(_cliente.Id, resultado.Id);
        Assert.Equal("Ana", resultado.Nombre);
        Assert.Equal("Gold", resultado.Categoria);
    }

    /// <summary> Búsqueda por DNI: un documento con letras (antes flujo 3b de CU-10) se rechaza sin ir a la base. </summary>
    [Fact]
    public async Task BuscarPorDocumentoAsync_WithMalformedDocumento_ThrowsDocumentoInvalidoException()
    {
        await Assert.ThrowsAsync<DocumentoInvalidoException>(() => _sut.BuscarPorDocumentoAsync(new ClienteBusquedaDTO { Documento = "abc123" }));
        _clienteRepo.Verify(r => r.GetByDocumentoAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary> Búsqueda por DNI: un documento bien formado pero no registrado lanza ClienteNotFoundException. </summary>
    [Fact]
    public async Task BuscarPorDocumentoAsync_WithUnknownDocumento_ThrowsClienteNotFoundException()
    {
        _clienteRepo.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ClienteNotFoundException>(() => _sut.BuscarPorDocumentoAsync(new ClienteBusquedaDTO { Documento = "99999999" }));
    }
}
