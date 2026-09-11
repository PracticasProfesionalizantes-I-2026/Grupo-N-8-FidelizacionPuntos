using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Registro y consulta de auditoría (CU-21, RN-13/RN-19/RN-20). Se inyecta en
/// los demás services para dejar constancia de cada operación relevante.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>Deja constancia de una operación. No lanza excepciones de negocio: un fallo de auditoría no debe frenar la operación principal.</summary>
    Task RegistrarAsync(string tipoOperacion, ActorTipo actorTipo, Guid? actorId, string entidadAfectada, Guid? entidadId, string? detalle = null);

    /// <summary>Consulta con filtros opcionales (CU-21).</summary>
    Task<IReadOnlyList<AuditoriaResponseDTO>> ConsultarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId);

    /// <summary>Detalle de un registro puntual (CU-21). Devuelve `null` si no existe (el controller lo mapea a 404).</summary>
    Task<AuditoriaResponseDTO?> ObtenerPorIdAsync(Guid id);
}
