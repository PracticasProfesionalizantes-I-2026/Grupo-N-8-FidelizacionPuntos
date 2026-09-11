using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IAuditoriaService"/> (CU-21).</summary>
public class AuditoriaService(IAuditoriaRepository auditoriaRepository) : IAuditoriaService
{
    /// <summary>Inserta el registro de auditoría (RN-13, RN-19). Es de solo inserción por RN-20.</summary>
    public async Task RegistrarAsync(string tipoOperacion, ActorTipo actorTipo, Guid? actorId, string entidadAfectada, Guid? entidadId, string? detalle = null)
    {
        await auditoriaRepository.CreateAsync(new Auditoria
        {
            TipoOperacion = tipoOperacion,
            ActorTipo = actorTipo,
            ActorId = actorId,
            EntidadAfectada = entidadAfectada,
            EntidadId = entidadId,
            Detalle = detalle
        });
    }

    /// <summary>Aplica los filtros recibidos y mapea a DTO (CU-21).</summary>
    public async Task<IReadOnlyList<AuditoriaResponseDTO>> ConsultarAsync(DateTime? fechaDesde, DateTime? fechaHasta, string? tipoOperacion, Guid? actorId)
    {
        var registros = await auditoriaRepository.ConsultarAsync(fechaDesde, fechaHasta, tipoOperacion, actorId);
        return registros.Select(MapToResponseDTO).ToList();
    }

    /// <summary>Detalle de un registro puntual (CU-21).</summary>
    public async Task<AuditoriaResponseDTO?> ObtenerPorIdAsync(Guid id)
    {
        var registro = await auditoriaRepository.GetByIdAsync(id);
        return registro is null ? null : MapToResponseDTO(registro);
    }

    /// <summary>Traduce la entidad de persistencia al DTO público, sin exponer detalles internos de más.</summary>
    private static AuditoriaResponseDTO MapToResponseDTO(Auditoria a) => new()
    {
        Id = a.Id,
        Fecha = a.Fecha,
        TipoOperacion = a.TipoOperacion,
        ActorTipo = a.ActorTipo,
        ActorId = a.ActorId,
        EntidadAfectada = a.EntidadAfectada,
        EntidadId = a.EntidadId,
        Detalle = a.Detalle
    };
}
