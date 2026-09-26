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
    /// Lotes de acumulación/bono no vencidos y con saldo propio sin agotar,
    /// del más antiguo al más nuevo (RN-09). Solo lectura (`AsNoTracking`):
    /// hoy solo se usa para calcular el saldo (CU-04), no para modificar lotes.
    /// </summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        await context.Movimientos.AsNoTracking()
            .Where(m => m.ClienteId == clienteId
                     && (m.Tipo == TipoMovimiento.Acumulacion || m.Tipo == TipoMovimiento.BonoCumpleanos)
                     && m.PuntosDisponibles > 0
                     && (m.FechaVencimiento == null || m.FechaVencimiento >= fechaReferencia))
            .OrderBy(m => m.Fecha)
            .ToListAsync();

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
