using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.API.IntegrationTests.Fakes;

/// <summary>
/// Envuelve al repositorio real y, justo después de leer los lotes para un canje, ejecuta otra
/// operación (un canje "concurrente" en otro scope/DbContext). Reproduce de forma determinística la
/// carrera del doble gasto (CRIT-01): las dos peticiones leen el mismo saldo y una guarda primero.
/// </summary>
public class MovimientoRepositoryConCanjeConcurrente(IMovimientoRepository real, Func<Task> operacionConcurrente) : IMovimientoRepository
{
    /// <summary> Lee los lotes con el repositorio real y, antes de devolverlos, deja que la otra operación termine. </summary>
    public async Task<IReadOnlyList<Movimiento>> GetLotesParaCanjeAsync(int clienteId, DateTime fechaReferencia)
    {
        var lotes = await real.GetLotesParaCanjeAsync(clienteId, fechaReferencia);
        await operacionConcurrente();
        return lotes;
    }

    /// <summary> Delega en el repositorio real. </summary>
    public Task<Movimiento> CreateAsync(Movimiento entity) => real.CreateAsync(entity);

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
    public Task<IReadOnlyList<Movimiento>> BuscarAsync(int? clienteId, TipoMovimiento? tipo, DateTime? fechaDesde, DateTime? fechaHasta) =>
        real.BuscarAsync(clienteId, tipo, fechaDesde, fechaHasta);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<IReadOnlyList<Movimiento>> GetLotesVencidosAsync(DateTime fechaReferencia) => real.GetLotesVencidosAsync(fechaReferencia);

    /// <summary> Delega en el repositorio real. </summary>
    public Task<bool> ExisteBonoEnAnioAsync(int clienteId, int anio) => real.ExisteBonoEnAnioAsync(clienteId, anio);
}
