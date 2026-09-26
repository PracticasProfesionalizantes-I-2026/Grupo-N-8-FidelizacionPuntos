using FidelixAPI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Context;

/// <summary>
/// Contexto de EF Core del sistema Fidelix. Configura las relaciones con
/// `DeleteBehavior.Restrict` (ninguna baja de un padre borra en cascada a sus
/// hijos: las bajas del dominio son siempre lógicas, vía `Activo = false`) y
/// los índices únicos que exigen las reglas de negocio de unicidad.
/// </summary>
public class FidelixDbContext(DbContextOptions<FidelixDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Beneficio> Beneficios => Set<Beneficio>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    /// <summary>Configura índices únicos (RN-01, RN-23) y relaciones sin borrado en cascada.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Unicidad (RN-01: documento/email de cliente; RN-23: nombre de beneficio) ---
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Documento).IsUnique();
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<Beneficio>().HasIndex(b => b.Nombre).IsUnique();

        // --- Movimiento: hijo de Cliente y Beneficio, nunca los arrastra en su baja ---
        modelBuilder.Entity<Movimiento>()
            .HasOne(m => m.Cliente)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Movimiento>()
            .HasOne(m => m.Beneficio)
            .WithMany(b => b.Canjes)
            .HasForeignKey(m => m.BeneficioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
