using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auditoria;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IAuditoriaService (RN-13, RN-20, CU-21). </summary>
public class AuditoriaService(IAuditoriaRepository auditoriaRepository) : IAuditoriaService
{
    /// <summary> Mensaje del flujo 3a: el admin tiene que acotar la búsqueda. </summary>
    private const string MensajeSinCriterios = "Defina al menos un criterio de búsqueda.";
    /// <summary> Mensaje del flujo 4a. </summary>
    private const string MensajeSinResultados = "No existen resultados para los filtros seleccionados";

    /// <summary> Crea el registro con la fecha actual (RN-13). </summary>
    public async Task RegistrarAsync(string tipoOperacion, Guid? actorId, string actorRol, Guid? entidadId, string detalle)
    {
        await auditoriaRepository.CreateAsync(new Auditoria
        {
            Fecha = DateTime.UtcNow,
            TipoOperacion = tipoOperacion,
            ActorId = actorId,
            ActorRol = actorRol,
            EntidadId = entidadId,
            Detalle = detalle
        });
    }

    /// <summary> Sin criterios no consulta la base (flujo 3a); con criterios filtra y avisa si no hay resultados (4a). </summary>
    public async Task<ListadoResponseDTO<AuditoriaResponseDTO>> ConsultarAuditoriaAsync(AuditoriaFiltroDTO filtro)
    {
        if (filtro.SinCriterios())
        {
            return new ListadoResponseDTO<AuditoriaResponseDTO> { Items = [], Mensaje = MensajeSinCriterios };
        }

        var registros = await auditoriaRepository.BuscarAsync(filtro.FechaDesde, filtro.FechaHasta, filtro.TipoOperacion, filtro.ActorId);
        return ListadoResponseDTO<AuditoriaResponseDTO>.Crear(registros.Select(MapToResponseDTO).ToList(), MensajeSinResultados);
    }

    /// <summary> Detalle de un registro o 404. </summary>
    public async Task<AuditoriaResponseDTO> ObtenerPorIdAsync(Guid id)
    {
        var registro = await auditoriaRepository.GetByIdAsync(id)
            ?? throw new AuditoriaNotFoundException("El registro de auditoría no existe.");
        return MapToResponseDTO(registro);
    }

    /// <summary> Traduce la entidad al DTO público. </summary>
    private static AuditoriaResponseDTO MapToResponseDTO(Auditoria a) => new()
    {
        Id = a.Id,
        Fecha = a.Fecha,
        TipoOperacion = a.TipoOperacion,
        ActorId = a.ActorId,
        ActorRol = a.ActorRol,
        EntidadId = a.EntidadId,
        Detalle = a.Detalle
    };
}
