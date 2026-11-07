using FidelixAPI.BusinessLogic.Helpers;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IReglaAcumulacionService (CU-19). </summary>
public class ReglaAcumulacionService(IReglaAcumulacionRepository reglaRepository) : IReglaAcumulacionService
{
    /// <summary> Máximo de puntos por peso: hasta 2 dígitos enteros y 2 decimales (99,99). </summary>
    private const decimal PuntosPorMontoMaximo = 99.99m;
    /// <summary> Cantidad máxima de decimales de los puntos por peso. </summary>
    private const int DecimalesMaximos = 2;
    /// <summary> Mínimo de días que duran los puntos una vez disponibles (un mes). </summary>
    private const int DiasVigenciaMinimos = 30;
    /// <summary> Máximo de días que duran los puntos una vez disponibles (dos años). </summary>
    private const int DiasVigenciaMaximos = 730;

    /// <summary>
    /// Listar: todas las versiones, ordenadas por fecha de inicio descendente (la más nueva primero), con
    /// la forma común de los listados `{ mensaje, items }`; sin reglas no es error.
    /// </summary>
    public async Task<ListadoResponseDTO<ReglaAcumulacionResponseDTO>> ObtenerReglasAcumulacionAsync()
    {
        var reglas = await reglaRepository.GetAllAsync();
        var items = reglas
            .OrderByDescending(r => r.VigenciaDesde)
            .Select(MapToResponseDTO)
            .ToList();
        return ListadoResponseDTO<ReglaAcumulacionResponseDTO>.Crear(items, "No hay reglas de acumulación configuradas");
    }

    /// <summary> Detalle de una versión por id, o 404 si no existe (flujo 3c). </summary>
    public async Task<ReglaAcumulacionResponseDTO> ObtenerReglaAcumulacionPorIdAsync(int id)
    {
        var regla = await reglaRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("La regla de acumulación no existe.");

        return MapToResponseDTO(regla);
    }

    /// <summary>
    /// Alta: valores dentro de rango (3a), fecha de inicio de hoy en adelante (no se crean reglas con
    /// efecto retroactivo) y sin superponerse con otra (RN-18); registra la fecha de alta (RN-13).
    /// </summary>
    public async Task<ReglaAcumulacionResponseDTO> CrearReglaAcumulacionAsync(ReglaAcumulacionCreateDTO dto)
    {
        // El [Required] del DTO ya garantizó que los tres campos vienen: los .Value son seguros.
        var puntosPorMonto = dto.PuntosPorMonto!.Value;
        var diasVigencia = dto.DiasVigenciaPuntos!.Value;
        var vigenciaDesde = dto.VigenciaDesde!.Value.Date;

        // Fail fast: primero los chequeos en memoria, después la consulta a la base.
        ValidarValores(puntosPorMonto, diasVigencia);
        ValidarFechaNoPasada(vigenciaDesde);
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

    /// <summary>
    /// Modificación parcial: solo de reglas que todavía no entraron en vigencia (409 si ya empezó, porque
    /// cambiaría con qué regla se calcularon compras ya hechas); valida lo que vino y registra la fecha de
    /// modificación (RN-13). Para cambiar una regla vigente se crea una versión nueva con fecha futura.
    /// </summary>
    public async Task<ReglaAcumulacionResponseDTO> ActualizarReglaAcumulacionAsync(int id, ReglaAcumulacionUpdateDTO dto)
    {
        var regla = await reglaRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("La regla de acumulación no existe.");

        // Mismo criterio que usa la acumulación para elegir la regla vigente (VigenciaDesde <= ahora).
        if (regla.VigenciaDesde <= DateTime.UtcNow)
        {
            throw new ReglaAcumulacionVigenteException(
                "La regla ya está vigente y no se puede modificar: creá una versión nueva con fecha de inicio futura.");
        }

        // Para los campos que no vinieron se validan los valores actuales, que ya son válidos.
        ValidarValores(dto.PuntosPorMonto ?? regla.PuntosPorMonto, dto.DiasVigenciaPuntos ?? regla.DiasVigenciaPuntos);

        if (dto.VigenciaDesde is not null && dto.VigenciaDesde.Value.Date != regla.VigenciaDesde)
        {
            ValidarFechaNoPasada(dto.VigenciaDesde.Value.Date);
            await ValidarSinSuperposicionAsync(dto.VigenciaDesde.Value.Date, idActual: id);
            regla.VigenciaDesde = dto.VigenciaDesde.Value.Date;
        }

        if (dto.PuntosPorMonto is not null) regla.PuntosPorMonto = dto.PuntosPorMonto.Value;
        if (dto.DiasVigenciaPuntos is not null) regla.DiasVigenciaPuntos = dto.DiasVigenciaPuntos.Value;
        regla.FechaModificacion = DateTime.UtcNow;

        await reglaRepository.UpdateAsync(regla);
        return MapToResponseDTO(regla);
    }

    /// <summary>
    /// Flujo 3a: los puntos por peso tienen que estar entre 0,01 y 99,99 con 2 decimales como máximo, y
    /// los días de vigencia entre 30 y 730.
    /// </summary>
    private static void ValidarValores(decimal puntosPorMonto, int diasVigenciaPuntos)
    {
        if (puntosPorMonto <= 0)
        {
            throw new ReglaAcumulacionInvalidaException("Los puntos por monto deben ser mayores a 0.");
        }

        if (puntosPorMonto > PuntosPorMontoMaximo)
        {
            throw new ReglaAcumulacionInvalidaException("Los puntos por monto no pueden superar 99.99.");
        }

        // Si redondear a 2 decimales cambia el valor, es que tenía más de 2 (ej. 0.123 → 0.12).
        if (decimal.Round(puntosPorMonto, DecimalesMaximos) != puntosPorMonto)
        {
            throw new ReglaAcumulacionInvalidaException("Los puntos por monto pueden tener como máximo 2 decimales.");
        }

        if (diasVigenciaPuntos is < DiasVigenciaMinimos or > DiasVigenciaMaximos)
        {
            throw new ReglaAcumulacionInvalidaException(
                $"Los días de vigencia de los puntos deben estar entre {DiasVigenciaMinimos} y {DiasVigenciaMaximos}.");
        }
    }

    /// <summary>
    /// La fecha de inicio no puede ser anterior a hoy (hora Argentina): una regla con fecha pasada
    /// cambiaría con qué regla se calcularon compras ya registradas.
    /// </summary>
    private static void ValidarFechaNoPasada(DateTime vigenciaDesde)
    {
        if (DateOnly.FromDateTime(vigenciaDesde) < FechasNegocio.HoyArgentina())
        {
            throw new ReglaAcumulacionInvalidaException("La fecha de inicio de la regla no puede ser anterior a hoy.");
        }
    }

    /// <summary>
    /// RN-18: no puede haber dos versiones que empiecen el mismo día, porque ese día
    /// habría dos reglas vigentes a la vez. `idActual` excluye a la propia regla en la modificación.
    /// </summary>
    private async Task ValidarSinSuperposicionAsync(DateTime vigenciaDesde, int? idActual)
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
