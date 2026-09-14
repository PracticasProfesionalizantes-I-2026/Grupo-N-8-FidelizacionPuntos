using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.ReglaAcumulacion;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IReglaAcumulacionService"/> (CU-19).</summary>
public class ReglaAcumulacionService(IReglaAcumulacionRepository reglaAcumulacionRepository, IAuditoriaService auditoriaService) : IReglaAcumulacionService
{
    /// <summary>Lista todas las reglas registradas (cierre del gap de lectura detectado en el plan técnico).</summary>
    public async Task<IReadOnlyList<ReglaAcumulacionResponseDTO>> ObtenerTodasAsync()
    {
        var reglas = await reglaAcumulacionRepository.GetAllAsync();
        return reglas.Select(MapToResponseDTO).ToList();
    }

    /// <summary>Detalle de una regla puntual (CU-19).</summary>
    public async Task<ReglaAcumulacionResponseDTO> ObtenerPorIdAsync(Guid id)
    {
        var regla = await reglaAcumulacionRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("No se encontró la regla de acumulación.");
        return MapToResponseDTO(regla);
    }

    /// <summary>Alta: valida que no se solape con otra regla activa (RN-18) y que el lapso de vencimiento sea positivo (RF-22).</summary>
    public async Task<ReglaAcumulacionResponseDTO> CrearAsync(ReglaAcumulacionCreateDTO dto)
    {
        if (dto.DiasVigenciaPuntos <= 0)
        {
            throw new ReglaAcumulacionInvalidaException("El lapso de vencimiento de los puntos debe ser mayor a cero.");
        }

        await ValidarSinSolapamientoAsync(dto.VigenciaDesde, dto.VigenciaHasta, idAExcluir: null);

        var regla = await reglaAcumulacionRepository.CreateAsync(new ReglaAcumulacion
        {
            PuntosPorMonto = dto.PuntosPorMonto,
            VigenciaDesde = dto.VigenciaDesde,
            VigenciaHasta = dto.VigenciaHasta,
            DiasVigenciaPuntos = dto.DiasVigenciaPuntos,
            Activa = true
        });

        await auditoriaService.RegistrarAsync("ConfigurarReglaAcumulacion", ActorTipo.Admin, null, "ReglaAcumulacion", regla.Id, "Alta");
        return MapToResponseDTO(regla);
    }

    /// <summary>Modificación: revalida el solapamiento excluyendo la propia regla.</summary>
    public async Task<ReglaAcumulacionResponseDTO> ActualizarAsync(Guid id, ReglaAcumulacionUpdateDTO dto)
    {
        var regla = await reglaAcumulacionRepository.GetByIdAsync(id)
            ?? throw new ReglaAcumulacionNotFoundException("No se encontró la regla de acumulación.");

        var nuevaVigenciaDesde = dto.VigenciaDesde ?? regla.VigenciaDesde;
        var nuevaVigenciaHasta = dto.VigenciaHasta ?? regla.VigenciaHasta;
        await ValidarSinSolapamientoAsync(nuevaVigenciaDesde, nuevaVigenciaHasta, idAExcluir: id);

        if (dto.PuntosPorMonto is not null) regla.PuntosPorMonto = dto.PuntosPorMonto.Value;
        if (dto.DiasVigenciaPuntos is <= 0)
        {
            throw new ReglaAcumulacionInvalidaException("El lapso de vencimiento de los puntos debe ser mayor a cero.");
        }
        if (dto.DiasVigenciaPuntos is not null) regla.DiasVigenciaPuntos = dto.DiasVigenciaPuntos.Value;
        regla.VigenciaDesde = nuevaVigenciaDesde;
        regla.VigenciaHasta = nuevaVigenciaHasta;

        await reglaAcumulacionRepository.UpdateAsync(regla);
        await auditoriaService.RegistrarAsync("ConfigurarReglaAcumulacion", ActorTipo.Admin, null, "ReglaAcumulacion", id, "Modificación");
        return MapToResponseDTO(regla);
    }

    /// <summary>RN-18: no puede haber dos versiones activas con vigencias que se crucen.</summary>
    private async Task ValidarSinSolapamientoAsync(DateTime vigenciaDesde, DateTime? vigenciaHasta, Guid? idAExcluir)
    {
        var solapadas = await reglaAcumulacionRepository.GetActivasSolapadasAsync(vigenciaDesde, vigenciaHasta, idAExcluir);
        if (solapadas.Count > 0)
        {
            throw new ReglaAcumulacionInvalidaException("La vigencia se solapa con una regla ya activa.");
        }
    }

    /// <summary>Traduce la entidad al DTO público.</summary>
    private static ReglaAcumulacionResponseDTO MapToResponseDTO(ReglaAcumulacion r) => new()
    {
        Id = r.Id,
        PuntosPorMonto = r.PuntosPorMonto,
        VigenciaDesde = r.VigenciaDesde,
        VigenciaHasta = r.VigenciaHasta,
        Activa = r.Activa,
        DiasVigenciaPuntos = r.DiasVigenciaPuntos
    };
}
