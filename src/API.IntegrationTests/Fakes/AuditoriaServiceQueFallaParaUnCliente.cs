using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.DTOs.Comun;

namespace FidelixAPI.API.IntegrationTests.Fakes;

/// <summary>
/// Envuelve al service de auditoría real y falla solo al auditar un proceso de un cliente puntual,
/// DESPUÉS de que el registro quedó agregado al contexto (como pasaría si fallara el SaveChanges).
/// Sirve para probar que el error no se arrastra al guardado del cliente siguiente (CRIT-02).
/// </summary>
public class AuditoriaServiceQueFallaParaUnCliente(IAuditoriaService real, int clienteQueFalla) : IAuditoriaService
{
    /// <summary> Si el detalle es del cliente elegido, falla; si no, delega en el service real. </summary>
    public Task RegistrarAsync(string tipoOperacion, int? actorId, string actorRol, int? entidadId, string detalle) =>
        detalle.StartsWith($"Cliente {clienteQueFalla}:")
            ? throw new InvalidOperationException("Falla de auditoría simulada")
            : real.RegistrarAsync(tipoOperacion, actorId, actorRol, entidadId, detalle);

    /// <summary> Delega en el service real. </summary>
    public Task<ListadoResponseDTO<AuditoriaResponseDTO>> ConsultarAuditoriaAsync(AuditoriaFiltroDTO filtro) =>
        real.ConsultarAuditoriaAsync(filtro);

    /// <summary> Delega en el service real. </summary>
    public Task<AuditoriaResponseDTO> ObtenerPorIdAsync(int id) => real.ObtenerPorIdAsync(id);
}
