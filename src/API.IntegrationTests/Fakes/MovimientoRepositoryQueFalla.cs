using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.API.IntegrationTests.Fakes;

/// <summary>
/// Repositorio de movimientos que siempre falla, para simular una caída de la base en los
/// flujos de error de persistencia (CU-10 4a, CU-07 5a). Se inyecta solo en el test que lo usa.
/// </summary>
public class MovimientoRepositoryQueFalla : IMovimientoRepository
{
    /// <summary>Simula una falla de conexión.</summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia) =>
        throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesParaCanjeAsync(Guid clienteId, DateTime fechaReferencia) =>
        throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<Movimiento?> GetByIdAsync(Guid id) => throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<IReadOnlyList<Movimiento>> GetAllAsync() => throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<Movimiento> CreateAsync(Movimiento entity) => throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task UpdateAsync(Movimiento entity) => throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<IReadOnlyList<Movimiento>> BuscarAsync(Guid? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta) =>
        throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesVencidosAsync(DateTime fechaReferencia) =>
        throw new InvalidOperationException("Falla de conexión simulada");

    /// <summary>Simula una falla de conexión.</summary>
    public Task<bool> ExisteBonoEnAnioAsync(Guid clienteId, int anio) =>
        throw new InvalidOperationException("Falla de conexión simulada");
}
