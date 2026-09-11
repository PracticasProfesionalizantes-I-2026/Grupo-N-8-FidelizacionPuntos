using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Seed;

/// <summary>
/// Crea la base de datos (aplicando migraciones pendientes) y la puebla con
/// datos de prueba la primera vez que arranca la API, si está vacía. Es
/// idempotente: si ya hay clientes cargados, no vuelve a sembrar nada.
/// </summary>
public static class DbInitializer
{
    /// <summary>Punto de entrada llamado desde `Program.cs` al arrancar la API.</summary>
    public static async Task InicializarAsync(FidelixDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Clientes.AnyAsync())
        {
            return; // Ya hay datos: no se vuelve a sembrar.
        }

        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            Nombre = "Admin Fidelix",
            Email = "admin@fidelix.local",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
            Activo = true
        };

        var empleados = new List<Empleado>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Empleado Demo",
                Documento = "30111222",
                Email = "empleado.demo@fidelix.local",
                PasswordHash = PasswordHasher.Hash("Empleado123!"),
                Activo = true
            }
        };

        var clientes = new List<Cliente>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Cliente Demo",
                Documento = "40333444",
                Email = "cliente.demo@fidelix.local",
                PasswordHash = PasswordHasher.Hash("Cliente123!"),
                Telefono = "1155550000",
                FechaNacimiento = new DateTime(1995, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                Activo = true
            }
        };

        var productos = new List<Producto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Producto Demo A", Descripcion = "Producto de ejemplo", Precio = 1500m, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Producto Demo B", Descripcion = "Producto de ejemplo", Precio = 2500m, Activo = true }
        };

        var beneficios = new List<Beneficio>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Descuento 10%", Descripcion = "10% de descuento en la próxima compra", CostoPuntos = 100, Categoria = CategoriaBeneficio.Descuento, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Producto Gratis Demo", Descripcion = "Un producto gratis de regalo", CostoPuntos = 300, Categoria = CategoriaBeneficio.ProductoGratis, Activo = true },
            // Costo deliberadamente altísimo: sirve para probar el 409 de RN-08
            // (saldo insuficiente) sin depender de cuánto saldo dejaron otras
            // pruebas ya corridas (colección Bruno, tests de integración).
            new() { Id = Guid.NewGuid(), Nombre = "Beneficio Costo Alto (uso en pruebas)", Descripcion = "Beneficio de costo alto para probar RN-08", CostoPuntos = 999_999, Categoria = CategoriaBeneficio.Otro, Activo = true }
        };

        var reglaAcumulacion = new ReglaAcumulacion
        {
            Id = Guid.NewGuid(),
            PuntosPorMonto = 1m, // 1 punto por cada unidad de moneda gastada.
            VigenciaDesde = DateTime.UtcNow.Date,
            VigenciaHasta = null,
            DiasVigenciaPuntos = 365, // RF-22: valor de ejemplo, configurable por el admin.
            Activa = true
        };

        context.Admins.Add(admin);
        context.Empleados.AddRange(empleados);
        context.Clientes.AddRange(clientes);
        context.Productos.AddRange(productos);
        context.Beneficios.AddRange(beneficios);
        context.ReglasAcumulacion.Add(reglaAcumulacion);

        await context.SaveChangesAsync();
    }
}
