using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Movimiento;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>
/// Flujo completo de punto de venta a través del pipeline HTTP real: un
/// empleado registra una compra (CU-10) y el cliente consulta su saldo
/// (CU-04) y canjea un beneficio (CU-07), validando que el saldo se
/// descuenta correctamente (RN-05, RN-08, RN-09).
/// </summary>
public class PuntosFlowTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    public PuntosFlowTests(FidelixApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.InicializarBaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcumularYCanjear_WithValidFlow_UpdatesSaldoCorrectly()
    {
        var empleado = await _factory.ClienteAutenticadoAsync("empleado.demo@fidelix.local", "Empleado123!");
        var cliente = await _factory.ClienteAutenticadoAsync("cliente.demo@fidelix.local", "Cliente123!");

        // CU-10: el empleado registra una compra de 2000 -> 2000 puntos (regla sembrada: 1 punto por unidad).
        var respuestaAcumulacion = await empleado.PostAsJsonAsync("/api/movimientos/acumulaciones", new MovimientoAcumulacionCreateDTO
        {
            ClienteDocumento = "40333444",
            Items = [new ItemCompraDTO { Producto = "Producto Demo A", Cantidad = 1, Monto = 2000m }]
        });
        Assert.Equal(HttpStatusCode.Created, respuestaAcumulacion.StatusCode);

        // CU-04: el cliente consulta su saldo.
        var saldo = await cliente.GetFromJsonAsync<SaldoResponseDTO>("/api/clientes/me/saldo");
        Assert.Equal(2000, saldo!.PuntosDisponibles);

        // CU-06: consulta el catálogo para obtener el Id del beneficio sembrado.
        var beneficios = await cliente.GetFromJsonAsync<List<BeneficioResponseDTO>>("/api/beneficios");
        var descuento = beneficios!.Single(b => b.Nombre == "Descuento 10%"); // 100 puntos

        // CU-07: el cliente canjea el beneficio.
        var respuestaCanje = await cliente.PostAsJsonAsync("/api/canjes", new CanjeCreateDTO { BeneficioId = descuento.Id });
        Assert.Equal(HttpStatusCode.Created, respuestaCanje.StatusCode);

        // El saldo debe reflejar el descuento (RN-09, FIFO).
        var saldoFinal = await cliente.GetFromJsonAsync<SaldoResponseDTO>("/api/clientes/me/saldo");
        Assert.Equal(1900, saldoFinal!.PuntosDisponibles);
    }

    [Fact]
    public async Task RegistrarAcumulacion_WithUnknownDocumento_Returns404NotFound()
    {
        var empleado = await _factory.ClienteAutenticadoAsync("empleado.demo@fidelix.local", "Empleado123!");

        var respuesta = await empleado.PostAsJsonAsync("/api/movimientos/acumulaciones", new MovimientoAcumulacionCreateDTO
        {
            ClienteDocumento = "00000000",
            Items = [new ItemCompraDTO { Producto = "X", Cantidad = 1, Monto = 100m }]
        });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearCanje_WithInsufficientBalance_Returns409Conflict()
    {
        var cliente = await _factory.ClienteAutenticadoAsync("cliente.demo@fidelix.local", "Cliente123!");

        var beneficios = await cliente.GetFromJsonAsync<List<BeneficioResponseDTO>>("/api/beneficios");
        var beneficio = beneficios!.First(); // el cliente sembrado arranca con saldo 0.

        var respuesta = await cliente.PostAsJsonAsync("/api/canjes", new CanjeCreateDTO { BeneficioId = beneficio.Id });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task RegistrarAcumulacion_AsCliente_Returns403Forbidden()
    {
        // RN de autorización por rol: solo un empleado puede registrar acumulaciones (CU-10).
        var cliente = await _factory.ClienteAutenticadoAsync("cliente.demo@fidelix.local", "Cliente123!");

        var respuesta = await cliente.PostAsJsonAsync("/api/movimientos/acumulaciones", new MovimientoAcumulacionCreateDTO
        {
            ClienteDocumento = "40333444",
            Items = [new ItemCompraDTO { Producto = "X", Cantidad = 1, Monto = 100m }]
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
