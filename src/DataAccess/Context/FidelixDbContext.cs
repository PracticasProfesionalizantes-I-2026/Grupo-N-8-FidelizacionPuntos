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
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Beneficio> Beneficios => Set<Beneficio>();
    public DbSet<ReglaAcumulacion> ReglasAcumulacion => Set<ReglaAcumulacion>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<CodigoRecuperacion> CodigosRecuperacion => Set<CodigoRecuperacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    /// <summary>Configura índices únicos (RN-01, RN-23, RN-24) y relaciones sin borrado en cascada.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Unicidad (RN-01: documento/email único; RN-23/RN-24: nombre único) ---
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Documento).IsUnique();
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Email).IsUnique();

        modelBuilder.Entity<Empleado>().HasIndex(e => e.Documento).IsUnique();
        modelBuilder.Entity<Empleado>().HasIndex(e => e.Email).IsUnique();

        modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique();

        modelBuilder.Entity<Producto>().HasIndex(p => p.Nombre).IsUnique();
        modelBuilder.Entity<Beneficio>().HasIndex(b => b.Nombre).IsUnique();

        // --- Movimiento: hijo de Cliente/Empleado/Beneficio, nunca los arrastra en su baja ---
        modelBuilder.Entity<Movimiento>()
            .HasOne(m => m.Cliente)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Movimiento>()
            .HasOne(m => m.Empleado)
            .WithMany(e => e.Movimientos)
            .HasForeignKey(m => m.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Movimiento>()
            .HasOne(m => m.Beneficio)
            .WithMany(b => b.Canjes)
            .HasForeignKey(m => m.BeneficioId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CodigoRecuperacion: hijo opcional de Cliente/Empleado/Admin (exactamente uno) ---
        modelBuilder.Entity<CodigoRecuperacion>()
            .HasOne(cr => cr.Cliente)
            .WithMany(c => c.CodigosRecuperacion)
            .HasForeignKey(cr => cr.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CodigoRecuperacion>()
            .HasOne(cr => cr.Empleado)
            .WithMany(e => e.CodigosRecuperacion)
            .HasForeignKey(cr => cr.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CodigoRecuperacion>()
            .HasOne(cr => cr.Admin)
            .WithMany(a => a.CodigosRecuperacion)
            .HasForeignKey(cr => cr.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Producto: sin FK entrante hoy (no forma parte del detalle de compra
        // como entidad relacionada; el ítem de compra se carga por nombre en
        // MovimientoAcumulacionCreateDTO/ItemCompraDTO). Se deja la entidad y el
        // índice único de nombre listos para cuando el detalle de compra
        // referencie productos por Id en una fase posterior.
    }
}
