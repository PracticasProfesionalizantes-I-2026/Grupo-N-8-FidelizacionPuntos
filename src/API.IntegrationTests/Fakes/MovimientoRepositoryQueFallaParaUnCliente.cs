using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.API.IntegrationTests.Fakes;

/// <summary>
/// Envuelve al repositorio real y falla solo al guardar un movimiento de un cliente puntual. Sirve
/// para probar que los procesos automáticos (CU-22, CU-26) siguen con el resto cuando uno falla.
/// </summary>
public class MovimientoRepositoryQueFallaParaUnCliente(IMovimientoRepository real, int clienteQueFalla) : IMovimientoRepository
{
    /// <summary> Falla si el movimiento es del cliente elegido; si no, delega en el repositorio real. </summary>
    public Task<Movimiento> CreateAsync(Movimiento entity) =>
        entity.ClienteId == clienteQueFalla
            ? throw new InvalidOperationException("Falla de conexión simulada")
            : real.CreateAsync(entity);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<Movimiento?> GetByIdAsync(int id) => real.GetByIdAsync(id);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> GetAllAsync() => real.GetAllAsync();

    /// <summary> Delega en el repositorio real. </summary>
    public Task UpdateAsync(Movimiento entity) => real.UpdateAsync(entity);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesVigentesPorClienteAsync(int clienteId, DateTime fechaReferencia) =>
        real.GetLotesVigentesPorClienteAsync(clienteId, fechaReferencia);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesParaCanjeAsync(int clienteId, DateTime fechaReferencia) =>
        real.GetLotesParaCanjeAsync(clienteId, fechaReferencia);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> BuscarAsync(int? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta) =>
        real.BuscarAsync(clienteId, tipo, fechaDesde, fechaHasta);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesVencidosAsync(DateTime fechaReferencia) => real.GetLotesVencidosAsync(fechaReferencia);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<bool> ExisteBonoEnAnioAsync(int clienteId, int anio) => real.ExisteBonoEnAnioAsync(clienteId, anio);
}
