using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.DTOs.Comun;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Registro (RN-13) y consulta (CU-21) de la auditoría de operaciones.</summary>
public interface IAuditoriaService
{
    /// <summary>Registra una operación crítica. Nunca se modifica después (RN-20).</summary>
    Task RegistrarAsync(string tipoOperacion, Guid? actorId, string actorRol, Guid? entidadId, string detalle);

    /// <summary>Busca registros por los criterios indicados; sin ningún criterio devuelve una lista vacía (flujo 3a).</summary>
    Task<ListadoResponseDTO<AuditoriaResponseDTO>> ConsultarAuditoriaAsync(AuditoriaFiltroDTO filtro);

    /// <summary>Detalle de un registro. Lanza 404 si no existe.</summary>
    Task<AuditoriaResponseDTO> ObtenerPorIdAsync(Guid id);
}
