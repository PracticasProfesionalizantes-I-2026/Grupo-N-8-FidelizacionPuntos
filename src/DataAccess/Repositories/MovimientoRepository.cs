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

    /// <summary>
    /// Lotes de acumulación/bono no vencidos, ya disponibles (N2) y con saldo propio
    /// sin agotar, del más antiguo al más nuevo (RN-09). Solo lectura (`AsNoTracking`):
    /// se usa para calcular el saldo (CU-04, CU-10).
    /// </summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        await LotesVigentes(clienteId, fechaReferencia).AsNoTracking().ToListAsync();

    /// <summary>Mismos lotes, con tracking: el canje (CU-07) los modifica por FIFO y los guarda junto con el canje.</summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesParaCanjeAsync(Guid clienteId, DateTime fechaReferencia) =>
        await LotesVigentes(clienteId, fechaReferencia).ToListAsync();

    /// <summary>Aplica solo los filtros informados, en orden cronológico (RN-06), incluyendo el beneficio.</summary>
    public async Task<IReadOnlyList<Movimiento>> BuscarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = context.Movimientos.AsNoTracking().Include(m => m.Beneficio).AsQueryable();

        if (clienteId is not null) query = query.Where(m => m.ClienteId == clienteId);
        if (tipo is not null) query = query.Where(m => m.Tipo == tipo);
        if (fechaDesde is not null) query = query.Where(m => m.Fecha >= fechaDesde);
        if (fechaHasta is not null) query = query.Where(m => m.Fecha <= fechaHasta);

        return await query.OrderBy(m => m.Fecha).ToListAsync();
    }

    /// <summary>Lotes de acumulación/bono con puntos sin consumir y ya vencidos, del más antiguo al más nuevo (FIFO, RN-09).</summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesVencidosAsync(DateTime fechaReferencia) =>
        await context.Movimientos
            .Where(m => (m.Tipo == TipoMovimiento.Acumulacion || m.Tipo == TipoMovimiento.BonoCumpleanos)
                     && m.PuntosDisponibles > 0
                     && m.FechaVencimiento != null && m.FechaVencimiento < fechaReferencia)
            .OrderBy(m => m.Fecha)
            .ToListAsync();

    /// <summary>Busca un bono de cumpleaños del cliente con fecha dentro del año indicado.</summary>
    public Task<bool> ExisteBonoEnAnioAsync(Guid clienteId, int anio)
    {
        var desde = new DateTime(anio, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = desde.AddYears(1);
        return context.Movimientos.AnyAsync(m => m.ClienteId == clienteId
                                              && m.Tipo == TipoMovimiento.BonoCumpleanos
                                              && m.Fecha >= desde && m.Fecha < hasta);
    }

    /// <summary>
    /// Consulta común a los dos métodos de lotes (se arma una sola vez para que el saldo
    /// y el canje usen exactamente el mismo criterio): lotes del cliente, de tipo
    /// acumulación/bono, con puntos sin consumir, no vencidos (RN-05/RN-21) y ya
    /// disponibles (N2), ordenados del más antiguo al más nuevo (FIFO, RN-09).
    /// </summary>
    private IQueryable<Movimiento> LotesVigentes(Guid clienteId, DateTime fechaReferencia) =>
        context.Movimientos
            .Where(m => m.ClienteId == clienteId
                     && (m.Tipo == TipoMovimiento.Acumulacion || m.Tipo == TipoMovimiento.BonoCumpleanos)
                     && m.PuntosDisponibles > 0
                     && (m.FechaVencimiento == null || m.FechaVencimiento >= fechaReferencia)
                     && (m.FechaDisponible == null || m.FechaDisponible <= fechaReferencia))
            .OrderBy(m => m.Fecha);

    /// <summary>Asigna un nuevo Id y persiste el movimiento.</summary>
    public async Task<Movimiento> CreateAsync(Movimiento entity)
    {
        entity.Id = Guid.NewGuid();
        context.Movimientos.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>Persiste los cambios sobre un movimiento ya trackeado.</summary>
    public Task UpdateAsync(Movimiento entity) => context.SaveChangesAsync();
}
