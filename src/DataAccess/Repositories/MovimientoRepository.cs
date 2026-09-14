using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IMovimientoRepository"/>.</summary>
public class MovimientoRepository(FidelixDbContext context) : IMovimientoRepository
{
    /// <summary>Busca un movimiento por Id, con tracking (permite modificarlo y guardarlo después).</summary>
    public Task<Movimiento?> GetByIdAsync(Guid id) =>
        context.Movimientos.FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>Lista todos los movimientos en modo solo lectura.</summary>
    public async Task<IReadOnlyList<Movimiento>> GetAllAsync() =>
        await context.Movimientos.AsNoTracking().ToListAsync();

    /// <summary>Historial de un cliente, del más reciente al más antiguo (RN-06).</summary>
    public async Task<IReadOnlyList<Movimiento>> GetByClienteIdAsync(Guid clienteId) =>
        await context.Movimientos.AsNoTracking()
            .Where(m => m.ClienteId == clienteId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

    /// <summary>
    /// Lotes de acumulación/bono no vencidos y con saldo propio sin agotar,
    /// del más antiguo al más nuevo (RN-09, FIFO): base para saldo disponible
    /// (RN-05) y descuento en canje.
    /// </summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        await context.Movimientos
            .Where(m => m.ClienteId == clienteId
                     && (m.Tipo == TipoMovimiento.Acumulacion || m.Tipo == TipoMovimiento.BonoCumpleanos)
                     && m.PuntosDisponibles > 0
                     && (m.FechaVencimiento == null || m.FechaVencimiento >= fechaReferencia))
            .OrderBy(m => m.Fecha)
            .ToListAsync();

    /// <summary>Consulta admin con filtros opcionales (CU-20).</summary>
    public async Task<IReadOnlyList<Movimiento>> ConsultarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = context.Movimientos.AsNoTracking().AsQueryable();

        if (clienteId is not null) query = query.Where(m => m.ClienteId == clienteId);
        if (tipo is not null) query = query.Where(m => m.Tipo == tipo);
        if (fechaDesde is not null) query = query.Where(m => m.Fecha >= fechaDesde);
        if (fechaHasta is not null) query = query.Where(m => m.Fecha <= fechaHasta);

        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    /// <summary>Ya existe un bono de cumpleaños para ese cliente en el año dado (RN-29).</summary>
    public Task<bool> ExisteBonoCumpleanosEnAnioAsync(Guid clienteId, int anio) =>
        context.Movimientos.AsNoTracking()
            .AnyAsync(m => m.ClienteId == clienteId && m.Tipo == TipoMovimiento.BonoCumpleanos && m.Fecha.Year == anio);

    /// <summary>Lotes vencidos con saldo pendiente, de cualquier cliente (CU-22). Con tracking, para poder darlos de baja.</summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesVencidosConSaldoAsync(DateTime fechaReferencia) =>
        await context.Movimientos
            .Where(m => (m.Tipo == TipoMovimiento.Acumulacion || m.Tipo == TipoMovimiento.BonoCumpleanos)
                     && m.PuntosDisponibles > 0
                     && m.FechaVencimiento != null && m.FechaVencimiento < fechaReferencia)
            .ToListAsync();

    /// <summary>Asigna un nuevo Id y persiste el movimiento.</summary>
    public async Task<Movimiento> CreateAsync(Movimiento entity)
    {
        entity.Id = Guid.NewGuid();
        context.Movimientos.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un movimiento (o lote) ya trackeado.</summary>
    public Task UpdateAsync(Movimiento entity) => context.SaveChangesAsync();
}
