using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IReglaAcumulacionService (CU-19). </summary>
public class ReglaAcumulacionService(IReglaAcumulacionRepository reglaRepository) : IReglaAcumulacionService
{
    /// <summary> Listar: todas las versiones, ordenadas por fecha de inicio descendente (la más nueva primero). </summary>
    public async Task<IReadOnlyList<ReglaAcumulacionResponseDTO>> ObtenerReglasAcumulacionAsync()
    {
        var reglas = await reglaRepository.GetAllAsync();
        return reglas
            .OrderByDescending(r => r.VigenciaDesde)
            .Select(MapToResponseDTO)
            .ToList();
    }

    /// <summary> Detalle de una versión por id, o 404 si no existe (flujo 3c). </summary>
    public async Task<ReglaAcumulacionResponseDTO> ObtenerReglaAcumulacionPorIdAsync(Guid id)
    {
        var regla = await reglaRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("La regla de acumulación no existe.");

        return MapToResponseDTO(regla);
    }

    /// <summary> Alta: valores positivos y fecha de inicio libre (RN-18); registra la fecha de alta (RN-13). </summary>
    public async Task<ReglaAcumulacionResponseDTO> CrearReglaAcumulacionAsync(ReglaAcumulacionCreateDTO dto)
    {
        // El [Required] del DTO ya garantizó que los tres campos vienen: los .Value son seguros.
        var puntosPorMonto = dto.PuntosPorMonto!.Value;
        var diasVigencia = dto.DiasVigenciaPuntos!.Value;
        var vigenciaDesde = dto.VigenciaDesde!.Value.Date;

        // Fail fast: primero los chequeos en memoria, después la consulta a la base.
        ValidarValoresPositivos(puntosPorMonto, diasVigencia);
        await ValidarSinSuperposicionAsync(vigenciaDesde, idActual: null);

        var regla = await reglaRepository.CreateAsync(new ReglaAcumulacion
        {
            PuntosPorMonto = puntosPorMonto,
            VigenciaDesde = vigenciaDesde,
            DiasVigenciaPuntos = diasVigencia,
            FechaCreacion = DateTime.UtcNow
        });

        return MapToResponseDTO(regla);
    }

    /// <summary> Modificación parcial: valida solo lo que vino; registra la fecha de modificación (RN-13). </summary>
    public async Task<ReglaAcumulacionResponseDTO> ActualizarReglaAcumulacionAsync(Guid id, ReglaAcumulacionUpdateDTO dto)
    {
        var regla = await reglaRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("La regla de acumulación no existe.");

        // Para los campos que no vinieron se validan los valores actuales, que ya son válidos.
        ValidarValoresPositivos(dto.PuntosPorMonto ?? regla.PuntosPorMonto, dto.DiasVigenciaPuntos ?? regla.DiasVigenciaPuntos);

        if (dto.VigenciaDesde is not null && dto.VigenciaDesde.Value.Date != regla.VigenciaDesde)
        {
            await ValidarSinSuperposicionAsync(dto.VigenciaDesde.Value.Date, idActual: id);
            regla.VigenciaDesde = dto.VigenciaDesde.Value.Date;
        }

        if (dto.PuntosPorMonto is not null) regla.PuntosPorMonto = dto.PuntosPorMonto.Value;
        if (dto.DiasVigenciaPuntos is not null) regla.DiasVigenciaPuntos = dto.DiasVigenciaPuntos.Value;
        regla.FechaModificacion = DateTime.UtcNow;

        await reglaRepository.UpdateAsync(regla);
        return MapToResponseDTO(regla);
    }

    /// <summary> Flujo 3a: los puntos por monto y los días de vigencia tienen que ser mayores a 0. </summary>
    private static void ValidarValoresPositivos(decimal puntosPorMonto, int diasVigenciaPuntos)
    {
        if (puntosPorMonto <= 0)
        {
            throw new ReglaAcumulacionInvalidaException("Los puntos por monto deben ser mayores a 0.");
        }

        if (diasVigenciaPuntos <= 0)
        {
            throw new ReglaAcumulacionInvalidaException("Los días de vigencia de los puntos deben ser mayores a 0.");
        }
    }

    /// <summary>
    /// RN-18: no puede haber dos versiones que empiecen el mismo día, porque ese día
    /// habría dos reglas vigentes a la vez. `idActual` excluye a la propia regla en la modificación.
    /// </summary>
    private async Task ValidarSinSuperposicionAsync(DateTime vigenciaDesde, Guid? idActual)
    {
        var otra = await reglaRepository.GetByVigenciaDesdeAsync(vigenciaDesde);
        if (otra is not null && otra.Id != idActual)
        {
            throw new ReglaAcumulacionInvalidaException("Ya existe una regla que entra en vigencia en esa fecha.");
        }
    }

    /// <summary> Traduce la entidad al DTO público. </summary>
    private static ReglaAcumulacionResponseDTO MapToResponseDTO(ReglaAcumulacion r) => new()
    {
        Id = r.Id,
        PuntosPorMonto = r.PuntosPorMonto,
        VigenciaDesde = r.VigenciaDesde,
        DiasVigenciaPuntos = r.DiasVigenciaPuntos,
        FechaCreacion = r.FechaCreacion,
        FechaModificacion = r.FechaModificacion
    };
}
