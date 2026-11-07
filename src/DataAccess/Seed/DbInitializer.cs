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
    // Todos los datos sembrados llevan su id explícito: si se mezclaran ids fijos con ids generados
    // por la base, una fila generada podría tomar el 1 y chocar con uno fijo. SQLite continúa la
    // numeración desde el máximo sembrado para las filas nuevas. Cada tabla numera por separado,
    // por eso el cliente, el admin y el empleado demo pueden tener todos el id 1.

    /// <summary>
    /// Id fijo del cliente de prueba, para poder consultar su saldo (CU-04)
    /// desde Bruno o Scalar sin tener que buscarlo antes en la base.
    /// </summary>
    public const int ClienteDemoId = 1;

    /// <summary>Id fijo del cliente Gold de prueba (N1).</summary>
    public const int ClienteGoldId = 3;

    /// <summary>Id fijo del admin de prueba (CU-14), usado también por los tests de integración.</summary>
    public const int AdminDemoId = 1;

    /// <summary>Id fijo del empleado de prueba (CU-09), usado también por los tests de integración.</summary>
    public const int EmpleadoDemoId = 1;

    /// <summary>Punto de entrada llamado desde `Program.cs` al arrancar la API.</summary>
    public static async Task InicializarAsync(FidelixDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Clientes.AnyAsync())
        {
            return; // Ya hay datos: no se vuelve a sembrar.
        }

        var hoy = DateTime.UtcNow;

        var cliente = new Cliente
        {
            Id = ClienteDemoId,
            Nombre = "Cliente Demo",
            Documento = "40333444",
            Email = "cliente.demo@fidelix.local",
            PasswordHash = PasswordHasher.Hash("Cliente123!"),
            Telefono = "1155550000",
            FechaNacimiento = new DateTime(1995, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Activo = true
        };

        var clienteInactivo = new Cliente
        {
            Id = 2,
            Nombre = "Cliente Inactivo",
            Documento = "54433304",
            Email = "cliente.inactivo@fidelix.local",
            PasswordHash = PasswordHasher.Hash("Inactivo123!"),
            Telefono = "1125550000",
            FechaNacimiento = new DateTime(2000, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            Activo = false
        };

        // Cliente Gold (N1): acumula x1.5. Sin movimientos, para ver en la demo que
        // sus puntos nuevos quedan pendientes 15 días (N2) y todavía no se pueden canjear.
        var clienteGold = new Cliente
        {
            Id = ClienteGoldId,
            Nombre = "Cliente Gold",
            Documento = "40111222",
            Email = "cliente.gold@fidelix.local",
            PasswordHash = PasswordHasher.Hash("Gold1234!"),
            Telefono = "1135550000",
            FechaNacimiento = new DateTime(1990, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            Activo = true,
            Categoria = CategoriaCliente.Gold
        };

        var beneficios = new List<Beneficio>
        {
            new() { Id = 1, Nombre = "Descuento 10%", Descripcion = "10% de descuento en la próxima compra", CostoPuntos = 100, Categoria = CategoriaBeneficio.Descuento, Activo = true },
            new() { Id = 2, Nombre = "Producto Gratis Demo", Descripcion = "Un producto gratis de regalo", CostoPuntos = 300, Categoria = CategoriaBeneficio.ProductoGratis, Activo = true }
        };

        // Lotes de puntos del cliente demo: dos vigentes (100 + 50) y uno ya
        // vencido (80), para que el saldo de CU-04 dé 150 y se vea que los
        // lotes vencidos no suman (RN-05).
        var movimientos = new List<Movimiento>
        {
            new() { Id = 1, ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 100, PuntosDisponibles = 100, Fecha = hoy.AddDays(-2), FechaVencimiento = hoy.AddDays(5), Detalle = "Compra de prueba" },
            new() { Id = 2, ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 50, PuntosDisponibles = 50, Fecha = hoy.AddDays(-1), FechaVencimiento = hoy.AddDays(6), Detalle = "Compra de prueba" },
            new() { Id = 3, ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 80, PuntosDisponibles = 80, Fecha = hoy.AddDays(-10), FechaVencimiento = hoy.AddDays(-3), Detalle = "Compra de prueba (lote vencido)" }
        };

        // Regla de acumulación vigente desde hace un mes (CU-19): 1 punto cada $10 y
        // los puntos vencen a los 180 días. Es la que va a usar CU-10 al acumular.
        var regla = new ReglaAcumulacion
        {
            Id = 1,
            PuntosPorMonto = 0.1m,
            VigenciaDesde = hoy.Date.AddDays(-30),
            DiasVigenciaPuntos = 180,
            FechaCreacion = hoy
        };

        // Personal de prueba: un admin (CU-14), un empleado activo (CU-09) y uno dado de baja (RN-15).
        var admin = new Admin
        {
            Id = AdminDemoId,
            Nombre = "Admin Demo",
            Email = "admin@fidelix.local",
            PasswordHash = PasswordHasher.Hash("Admin123!")
        };

        var empleados = new List<Empleado>
        {
            new() { Id = EmpleadoDemoId, Nombre = "Empleado Demo", Documento = "30111222", Email = "empleado@fidelix.local", PasswordHash = PasswordHasher.Hash("Empleado123!"), Activo = true },
            new() { Id = 2, Nombre = "Empleado Inactivo", Documento = "30333444", Email = "empleado.inactivo@fidelix.local", PasswordHash = PasswordHasher.Hash("Inactivo123!"), Activo = false }
        };

        context.Admins.Add(admin);
        context.Empleados.AddRange(empleados);
        context.Clientes.Add(cliente);
        context.Clientes.Add(clienteInactivo);
        context.Clientes.Add(clienteGold);
        context.Beneficios.AddRange(beneficios);
        context.Movimientos.AddRange(movimientos);
        context.ReglasAcumulacion.Add(regla);

        await context.SaveChangesAsync();
    }
}
