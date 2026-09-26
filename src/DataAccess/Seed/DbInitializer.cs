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
    /// <summary>
    /// Id fijo del cliente de prueba, para poder consultar su saldo (CU-04)
    /// desde Bruno o Scalar sin tener que buscarlo antes en la base.
    /// </summary>
    public static readonly Guid ClienteDemoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        var beneficios = new List<Beneficio>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Descuento 10%", Descripcion = "10% de descuento en la próxima compra", CostoPuntos = 100, Categoria = CategoriaBeneficio.Descuento, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Producto Gratis Demo", Descripcion = "Un producto gratis de regalo", CostoPuntos = 300, Categoria = CategoriaBeneficio.ProductoGratis, Activo = true }
        };

        // Lotes de puntos del cliente demo: dos vigentes (100 + 50) y uno ya
        // vencido (80), para que el saldo de CU-04 dé 150 y se vea que los
        // lotes vencidos no suman (RN-05).
        var movimientos = new List<Movimiento>
        {
            new() { Id = Guid.NewGuid(), ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 100, PuntosDisponibles = 100, Fecha = hoy.AddDays(-2), FechaVencimiento = hoy.AddDays(5), Detalle = "Compra de prueba" },
            new() { Id = Guid.NewGuid(), ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 50, PuntosDisponibles = 50, Fecha = hoy.AddDays(-1), FechaVencimiento = hoy.AddDays(6), Detalle = "Compra de prueba" },
            new() { Id = Guid.NewGuid(), ClienteId = ClienteDemoId, Tipo = TipoMovimiento.Acumulacion, Puntos = 80, PuntosDisponibles = 80, Fecha = hoy.AddDays(-10), FechaVencimiento = hoy.AddDays(-3), Detalle = "Compra de prueba (lote vencido)" }
        };

        context.Clientes.Add(cliente);
        context.Beneficios.AddRange(beneficios);
        context.Movimientos.AddRange(movimientos);

        await context.SaveChangesAsync();
    }
}
