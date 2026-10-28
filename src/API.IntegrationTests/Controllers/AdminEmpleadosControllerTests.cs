using System.Net;
using System.Net.Http.Json;
using FidelixAPI.API.IntegrationTests.Fakes;
using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Repositories;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.DTOs.Procesos;
using FidelixAPI.Shared.DTOs.Producto;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FidelixAPI.API.IntegrationTests.Controllers;

/// <summary>Tests de integración del ABM de empleados por el admin (`AdminEmpleadosController`, CU-16).</summary>
public class AdminEmpleadosControllerTests : IClassFixture<FidelixApiFactory>, IAsyncLifetime
{
    private const string Url = "/api/admin/empleados";

    private readonly FidelixApiFactory _factory;

    /// <summary>Recibe la factory compartida por todos los tests de la clase.</summary>
    public AdminEmpleadosControllerTests(FidelixApiFactory factory) => _factory = factory;

    /// <summary>Antes de cada test: aplica migraciones y siembra datos (idempotente).</summary>
    public Task InitializeAsync() => _factory.InicializarBaseAsync();

    /// <summary>No hay nada que limpiar: la base en memoria vive lo que vive la factory.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Datos válidos de un empleado nuevo con documento y email únicos.</summary>
    private static EmpleadoCreateDTO Nuevo()
    {
        var documento = TestHelpers.DocumentoUnico();
        return new EmpleadoCreateDTO { Nombre = "Empleado Nuevo", Documento = documento, Email = $"emp.{documento}@fidelix.local", Password = "Empleado123!" };
    }

    /// <summary>Crea (como admin) un empleado nuevo y devuelve su DTO.</summary>
    private async Task<EmpleadoResponseDTO> CrearEmpleadoAsync(EmpleadoCreateDTO dto) =>
        (await (await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, dto)).Content.ReadFromJsonAsync<EmpleadoResponseDTO>())!;

    /// <summary>CU-16, alta: devuelve 201 y el empleado puede iniciar sesión (CU-09).</summary>
    [Fact]
    public async Task AbmEmpleado_Create_WithValidData_Returns201Created()
    {
        var dto = Nuevo();

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, dto);
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/empleados/login",
            new FidelixAPI.Shared.DTOs.Auth.LoginRequestDTO { Email = dto.Email, Password = dto.Password });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    /// <summary>CU-16, modificación: devuelve 200 con el nombre nuevo.</summary>
    [Fact]
    public async Task AbmEmpleado_Update_WithValidData_Returns200OK()
    {
        var empleado = await CrearEmpleadoAsync(Nuevo());

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PutAsJsonAsync($"{Url}/{empleado.Id}", new EmpleadoUpdateDTO { Nombre = "Nombre Cambiado" });
        var actualizado = await respuesta.Content.ReadFromJsonAsync<EmpleadoResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Nombre Cambiado", actualizado!.Nombre);
    }

    /// <summary>CU-16, baja: devuelve 204 y el empleado ya no puede entrar (403, RN-15).</summary>
    [Fact]
    public async Task AbmEmpleado_Delete_WithExistingId_Returns204NoContent()
    {
        var dto = Nuevo();
        var empleado = await CrearEmpleadoAsync(dto);

        var respuesta = await _factory.CreateClientComo(Roles.Admin).DeleteAsync($"{Url}/{empleado.Id}");
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/empleados/login",
            new FidelixAPI.Shared.DTOs.Auth.LoginRequestDTO { Email = dto.Email, Password = dto.Password });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    /// <summary>CU-16, flujo 2a: un email inválido devuelve 400.</summary>
    [Fact]
    public async Task AbmEmpleado_WithInvalidData_Returns400BadRequest()
    {
        var dto = Nuevo();
        dto.Email = "no-es-un-email";

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, dto);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>CU-16, flujo 3a: el documento del empleado sembrado ya existe (409, RN-01).</summary>
    [Fact]
    public async Task AbmEmpleado_Create_WhenDocumentoExists_Returns409Conflict()
    {
        var dto = Nuevo();
        dto.Documento = "30111222";

        var respuesta = await _factory.CreateClientComo(Roles.Admin).PostAsJsonAsync(Url, dto);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    /// <summary>CU-16, flujo 3b: modificar un id inexistente devuelve 404.</summary>
    [Fact]
    public async Task AbmEmpleado_Update_WithNonExistentId_Returns404NotFound()
    {
        var respuesta = await _factory.CreateClientComo(Roles.Admin).PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", new EmpleadoUpdateDTO { Nombre = "X" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
