using System.Net.Http.Headers;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.Enums;
using FidelixAPI.DataAccess.Seed;
using FidelixAPI.Shared.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FidelixAPI.API.IntegrationTests;

/// <summary>
/// `WebApplicationFactory` que levanta la API real (todo el pipeline HTTP
/// y la DI) contra una base SQLite en memoria propia por instancia, en vez
/// de la `fidelix.db` de desarrollo. Cada test class que la usa (`IClassFixture`)
/// comparte la misma base durante toda la clase.
/// </summary>
public class FidelixApiFactory : WebApplicationFactory<Program>
{
    // SQLite ":memory:" solo persiste mientras haya al menos una conexión
    // abierta; se mantiene esta abierta durante toda la vida de la factory.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>Reemplaza la base SQLite de desarrollo por una en memoria, propia de esta factory.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        // Los procesos diarios (CU-22, CU-26) no corren en los tests: cambiarían los datos sembrados.
        builder.UseSetting("Jobs:Habilitados", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FidelixDbContext>>();
            services.AddDbContext<FidelixDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Aplica migraciones y siembra los datos de prueba (mismo `DbInitializer` que usa la app real).</summary>
    public async Task InicializarBaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FidelixDbContext>();
        await DbInitializer.InicializarAsync(context);
    }

    /// <summary>
    /// Crea un cliente HTTP que ya manda el token del rol indicado (Admin, Empleado o Cliente),
    /// a nombre de la cuenta de prueba sembrada de ese rol. Evita repetir el login en cada test.
    /// </summary>
    public HttpClient CreateClientComo(string rol)
    {
        var client = CreateClient();
        Autenticar(client, rol);
        return client;
    }

    /// <summary>Agrega a un cliente HTTP existente el header `Authorization: Bearer` del rol indicado.</summary>
    public void Autenticar(HttpClient client, string rol)
    {
        var id = rol switch
        {
            Roles.Admin => DbInitializer.AdminDemoId,
            Roles.Empleado => DbInitializer.EmpleadoDemoId,
            _ => DbInitializer.ClienteDemoId
        };

        using var scope = Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerarToken(id, rol);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Inserta directo en la base un lote de puntos del cliente, para preparar un test sin depender
    /// de otros CU. Por defecto queda disponible y vigente; con `fechaVencimiento` en el pasado sirve
    /// para probar el vencimiento (CU-22).
    /// </summary>
    public async Task<Movimiento> AcreditarPuntosAsync(Guid clienteId, int puntos, DateTime? fechaVencimiento = null, DateTime? fecha = null)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FidelixDbContext>();
        var lote = new Movimiento
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Tipo = TipoMovimiento.Acumulacion,
            Puntos = puntos,
            PuntosDisponibles = puntos,
            Fecha = fecha ?? DateTime.UtcNow.AddDays(-1),
            FechaVencimiento = fechaVencimiento ?? DateTime.UtcNow.AddDays(180),
            Detalle = "Lote de prueba"
        };
        context.Movimientos.Add(lote);
        await context.SaveChangesAsync();
        return lote;
    }

    /// <summary>Cierra la conexión en memoria (y con ella, la base de prueba) al descartar la factory.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
