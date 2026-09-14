using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FidelixAPI.API.IntegrationTests;

/// <summary>
/// `WebApplicationFactory` que levanta la API real (todo el pipeline HTTP,
/// DI y JWT) contra una base SQLite en memoria propia por instancia, en vez
/// de la `fidelix.db` de desarrollo. Cada test class que la usa (`IClassFixture`)
/// comparte la misma base durante toda la clase.
/// </summary>
public class FidelixApiFactory : WebApplicationFactory<Program>
{
    // SQLite ":memory:" solo persiste mientras haya al menos una conexión
    // abierta; se mantiene esta abierta durante toda la vida de la factory.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FidelixDbContext>>();
            services.AddDbContext<FidelixDbContext>(options => options.UseSqlite(_connection));

            // Los jobs de Sistema (CU-22, CU-26) corren en segundo plano y
            // arrancan solos al levantar el host: en tests no queremos ese
            // trabajo real ejecutándose de fondo contra la base de prueba.
            services.RemoveAll<IHostedService>();
        });
    }

    /// <summary>Aplica migraciones y siembra los datos de prueba (mismo `DbInitializer` que usa la app real).</summary>
    public async Task InicializarBaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FidelixDbContext>();
        await DbInitializer.InicializarAsync(context);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
