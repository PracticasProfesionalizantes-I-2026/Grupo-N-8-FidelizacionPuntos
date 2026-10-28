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
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ReglaAcumulacion> ReglasAcumulacion => Set<ReglaAcumulacion>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<CodigoRecuperacion> CodigosRecuperacion => Set<CodigoRecuperacion>();

    /// <summary>Configura índices únicos (RN-01, RN-18, RN-23, RN-24) y relaciones sin borrado en cascada.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Unicidad (RN-01: documento/email de cliente; RN-23: nombre de beneficio; RN-24: nombre de producto) ---
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Documento).IsUnique();
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<Beneficio>().HasIndex(b => b.Nombre).IsUnique();
        modelBuilder.Entity<Producto>().HasIndex(p => p.Nombre).IsUnique();

        // --- RN-01 también para empleados y admins (cada uno en su tabla) ---
        modelBuilder.Entity<Empleado>().HasIndex(e => e.Documento).IsUnique();
        modelBuilder.Entity<Empleado>().HasIndex(e => e.Email).IsUnique();
        modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique();

        // --- Búsquedas frecuentes: auditoría por fecha (CU-21) y códigos por email (CU-25) ---
        modelBuilder.Entity<Auditoria>().HasIndex(a => a.Fecha);
        modelBuilder.Entity<CodigoRecuperacion>().HasIndex(c => c.Email);

        // --- RN-18: una sola versión de la regla de acumulación por fecha de inicio ---
        modelBuilder.Entity<ReglaAcumulacion>().HasIndex(r => r.VigenciaDesde).IsUnique();

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
