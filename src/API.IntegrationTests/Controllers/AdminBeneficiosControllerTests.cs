using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Enums;
using System.Net;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.Security;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración de `AdminBeneficiosController` (CU-17) a través del pipeline HTTP real.</summary>
public class AdminBeneficiosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminBeneficiosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Crea un beneficio con nombre único y devuelve su DTO (helper para los tests de modificación/baja).</summary>
    private async Task<BeneficioResponseDTO> CrearBeneficioAsync(HttpClient client)
    {
        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO
        {
            Nombre = $"Beneficio {Guid.NewGuid():N}",
            CostoPuntos = 200
        });
        return (await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>())!;
    }

    /// <summary>CU-17, alta: datos válidos devuelven 201 Created.</summary>
    [Fact]
    public async Task AbmBeneficio_Create_WithValidData_Returns201Created()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO
        {
            Nombre = "Café gratis",
            Descripcion = "Un café de regalo",
            CostoPuntos = 50
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    /// <summary>CU-17, flujo 2a: un nombre vacío devuelve 400 Bad Request (DataAnnotations).</summary>
    [Fact]
    public async Task AbmBeneficio_WithInvalidData_Returns400BadRequest()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = "", CostoPuntos = 50 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-17, flujo 3a: costo cero devuelve 400 Bad Request (RN-16).</summary>
    [Fact]
    public async Task AbmBeneficio_Create_WithNonPositiveCosto_Returns400BadRequest()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = "Costo Cero", CostoPuntos = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-17, flujo 3a: un costo de más de 6 dígitos enteros devuelve 400 Bad Request (RN-16).</summary>
    [Fact]
    public async Task AbmBeneficio_Create_WithCostoAboveMaximum_Returns400BadRequest()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = "Costo Excesivo", CostoPuntos = 1_000_000 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-17, flujo 3b: el nombre de un beneficio sembrado devuelve 409 Conflict (RN-23).</summary>
    [Fact]
    public async Task AbmBeneficio_Create_WhenNombreExists_Returns409Conflict()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PostAsJsonAsync("/api/admin/beneficios", new BeneficioCreateDTO { Nombre = "Descuento 10%", CostoPuntos = 100 });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-17, modificación: devuelve 200 OK con el costo actualizado.</summary>
    [Fact]
    public async Task AbmBeneficio_Update_WithValidData_Returns200OK()
    {
        var client = _factory.CreateClientComo(Roles.Admin);
        var beneficio = await CrearBeneficioAsync(client);

        var respuesta = await client.PutAsJsonAsync($"/api/admin/beneficios/{beneficio.Id}", new BeneficioUpdateDTO { CostoPuntos = 250 });
        var actualizado = await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(250, actualizado!.CostoPuntos);
    }

    /// <summary>CU-17, flujo 3c: modificar un Id inexistente devuelve 404 Not Found.</summary>
    [Fact]
    public async Task AbmBeneficio_Update_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateClientComo(Roles.Admin);

        var respuesta = await client.PutAsJsonAsync($"/api/admin/beneficios/{IdsDePrueba.Inexistente}", new BeneficioUpdateDTO { CostoPuntos = 250 });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>CU-17, desactivación: devuelve 200 OK con el beneficio inactivo (RN-17).</summary>
    [Fact]
    public async Task AbmBeneficio_Deactivate_WithExistingId_Returns200OK()
    {
        var client = _factory.CreateClientComo(Roles.Admin);
        var beneficio = await CrearBeneficioAsync(client);

        var respuesta = await client.PatchAsync($"/api/admin/beneficios/{beneficio.Id}/desactivar", null);
        var desactivado = await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False(desactivado!.Activo);
    }

    /// <summary>CU-17: el admin consulta las categorías válidas (`{ id, nombre }`, desde 1).</summary>
    [Fact]
    public async Task ListarCategorias_Returns200WithCategoriesStartingAtOne()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).GetAsync("/api/admin/beneficios/categorias");
        var categorias = await respuesta.Content.ReadFromJsonAsync<ListadoResponseDTO<CategoriaBeneficioDTO>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, categorias!.Items.First().Id);
        Assert.Equal(4, categorias.Items.Count);
    }

    /// <summary>
    /// CU-17: una categoría que no existe (los casos encontrados en Bruno: 0, -5 y 1234567890) da 400.
    /// Antes se guardaba, porque el JSON de .NET acepta cualquier entero para un enum.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1234567890)]
    public async Task AbmBeneficio_Create_WithUnknownCategoria_Returns400BadRequest(int categoria)
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync("/api/admin/beneficios",
            new BeneficioCreateDTO { Nombre = $"Categoria {Guid.NewGuid():N}", CostoPuntos = 10, Categoria = (CategoriaBeneficio)categoria });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-17: una categoría válida (2 = Producto gratis) se guarda y se devuelve con ese número.</summary>
    [Fact]
    public async Task AbmBeneficio_Create_WithValidCategoria_Returns201WithThatCategoria()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync("/api/admin/beneficios",
            new BeneficioCreateDTO { Nombre = $"Categoria {Guid.NewGuid():N}", CostoPuntos = 10, Categoria = CategoriaBeneficio.ProductoGratis });
        var beneficio = await respuesta.Content.ReadFromJsonAsync<BeneficioResponseDTO>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(2, (int)beneficio!.Categoria);
    }
}
