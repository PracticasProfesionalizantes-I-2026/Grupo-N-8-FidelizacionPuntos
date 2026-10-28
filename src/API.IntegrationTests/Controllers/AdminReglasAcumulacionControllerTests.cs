using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Security;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de `AdminReglasAcumulacionController` (CU-19) a través del pipeline HTTP real.</summary>
public class AdminReglasAcumulacionControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/admin/reglas-acumulacion";

    /// <summary>
    /// Contador de días para que cada regla creada en los tests tenga una fecha de inicio distinta:
    /// los tests comparten la misma base y dos reglas con la misma fecha chocarían por RN-18.
    /// </summary>
    private static int _diasOffset = 1000;

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminReglasAcumulacionControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Devuelve una fecha futura que ningún otro test usó todavía.</summary>
    private static DateTime FechaUnica() =>
        DateTime.UtcNow.Date.AddDays(Interlocked.Increment(ref _diasOffset));

    /// <summary>Crea una regla válida con fecha única y devuelve su DTO (helper para detalle, modificación y superposición).</summary>
    private static async Task<ReglaAcumulacionResponseDTO> CrearReglaAsync(HttpClient client)
    {
        var respuesta = await client.PostAsJsonAsync(Url, new ReglaAcumulacionCreateDTO
        {
            PuntosPorMonto = 0.1m,
            VigenciaDesde = FechaUnica(),
            DiasVigenciaPuntos = 180
        });
        return (await respuesta.Content.ReadFromJsonAsync<ReglaAcumulacionResponseDTO>())!;
    }

    /// <summary>CU-19, listar: devuelve 200 OK con al menos la regla sembrada.</summary>
    [Fact]
    public async Task ConsultarReglasAcumulacion_Returns200OK()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.GetAsync(Url);
        var reglas = await respuesta.Content.ReadFromJsonAsync<List<ReglaAcumulacionResponseDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotEmpty(reglas!);
    }

    /// <summary>CU-19, detalle: un id existente devuelve 200 OK con esa regla.</summary>
    [Fact]
    public async Task ConsultarReglaAcumulacionPorId_WithExistingId_Returns200OK()
    {
        var client = _factory.CreateClientComo(Roles.Admin);
        var regla = await CrearReglaAsync(client);

        var respuesta = await client.GetAsync($"{Url}/{regla.Id}");
        var detalle = await respuesta.Content.ReadFromJsonAsync<ReglaAcumulacionResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(regla.Id, detalle!.Id);
    }

    /// <summary>CU-19, flujo 3c: un id inexistente devuelve 404 Not Found.</summary>
    [Fact]
    public async Task ConsultarReglaAcumulacionPorId_WithUnknownId_Returns404NotFound()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.GetAsync($"{Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-19, alta: datos válidos devuelven 201 Created con la fecha de alta registrada (RN-13).</summary>
    [Fact]
    public async Task ConfigurarReglaAcumulacion_Create_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync(Url, new ReglaAcumulacionCreateDTO
        {
            PuntosPorMonto = 0.2m,
            VigenciaDesde = FechaUnica(),
            DiasVigenciaPuntos = 90
        });
        var creada = await respuesta.Content.ReadFromJsonAsync<ReglaAcumulacionResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotEqual(default, creada!.FechaCreacion);
    }

    /// <summary>CU-19, modificación: devuelve 200 OK con el valor nuevo y la fecha de modificación (RN-13).</summary>
    [Fact]
    public async Task ConfigurarReglaAcumulacion_Update_WithValidData_Returns200OK()
    {
        var client = _factory.CreateClientComo(Roles.Admin);
        var regla = await CrearReglaAsync(client);

        var respuesta = await client.PutAsJsonAsync($"{Url}/{regla.Id}", new ReglaAcumulacionUpdateDTO { PuntosPorMonto = 0.5m });
        var actualizada = await respuesta.Content.ReadFromJsonAsync<ReglaAcumulacionResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0.5m, actualizada!.PuntosPorMonto);
        Assert.NotNull(actualizada.FechaModificacion);
    }

    /// <summary>CU-19, flujo 3a: crear otra regla con la misma fecha de inicio devuelve 400 Bad Request (RN-18).</summary>
    [Fact]
    public async Task ConfigurarReglaAcumulacion_WithOverlappingActiveRule_Returns400BadRequest()
    {
        var client = _factory.CreateClientComo(Roles.Admin);
        var existente = await CrearReglaAsync(client);

        var respuesta = await client.PostAsJsonAsync(Url, new ReglaAcumulacionCreateDTO
        {
            PuntosPorMonto = 0.3m,
            VigenciaDesde = existente.VigenciaDesde,
            DiasVigenciaPuntos = 30
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-19, flujo 3b: un JSON sin los campos obligatorios devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task ConfigurarReglaAcumulacion_WithMissingFields_Returns400BadRequest()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync(Url, new { puntosPorMonto = 0.1m });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
