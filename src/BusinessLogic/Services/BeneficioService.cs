using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IBeneficioService"/> (CU-06, CU-17).</summary>
public class BeneficioService(IBeneficioRepository beneficioRepository, IAuditoriaService auditoriaService) : IBeneficioService
{
    public async Task<IReadOnlyList<BeneficioResponseDTO>> ConsultarActivosAsync()
    {
        var beneficios = await beneficioRepository.GetActivosAsync();
        return beneficios.Select(MapToResponseDTO).ToList();
    }

    /// <summary>Alta: nombre único (RN-23) y costo en puntos positivo (RN-16).</summary>
    public async Task<BeneficioResponseDTO> CrearAsync(BeneficioCreateDTO dto)
    {
        if (dto.CostoPuntos <= 0)
        {
            throw new CostoInvalidoException("El costo en puntos debe ser mayor a cero.");
        }

        if (await beneficioRepository.GetByNombreAsync(dto.Nombre) is not null)
        {
            throw new BeneficioDuplicadoException("Ya existe un beneficio con ese nombre.");
        }

        var beneficio = await beneficioRepository.CreateAsync(new Beneficio
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            CostoPuntos = dto.CostoPuntos,
            Categoria = dto.Categoria,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("CrearBeneficio", ActorTipo.Admin, null, "Beneficio", beneficio.Id);
        return MapToResponseDTO(beneficio);
    }

    public async Task<BeneficioResponseDTO> ActualizarAsync(Guid id, BeneficioUpdateDTO dto)
    {
        var beneficio = await beneficioRepository.GetByIdAsync(id)
            ?? throw new BeneficioNotFoundException("No se encontró el beneficio.");

        if (dto.CostoPuntos is <= 0)
        {
            throw new CostoInvalidoException("El costo en puntos debe ser mayor a cero.");
        }

        if (dto.Nombre is not null && dto.Nombre != beneficio.Nombre)
        {
            var otro = await beneficioRepository.GetByNombreAsync(dto.Nombre);
            if (otro is not null && otro.Id != id)
            {
                throw new BeneficioDuplicadoException("Ya existe un beneficio con ese nombre.");
            }

            beneficio.Nombre = dto.Nombre;
        }

        if (dto.Descripcion is not null) beneficio.Descripcion = dto.Descripcion;
        if (dto.CostoPuntos is not null) beneficio.CostoPuntos = dto.CostoPuntos.Value;
        if (dto.Categoria is not null) beneficio.Categoria = dto.Categoria.Value;

        await beneficioRepository.UpdateAsync(beneficio);
        await auditoriaService.RegistrarAsync("ActualizarBeneficio", ActorTipo.Admin, null, "Beneficio", id);
        return MapToResponseDTO(beneficio);
    }

    /// <summary>Desactivación lógica (RN-17): a partir de ahora, no se puede canjear.</summary>
    public async Task<BeneficioResponseDTO> DesactivarAsync(Guid id)
    {
        var beneficio = await beneficioRepository.GetByIdAsync(id)
            ?? throw new BeneficioNotFoundException("No se encontró el beneficio.");

        beneficio.Activo = false;
        await beneficioRepository.UpdateAsync(beneficio);
        await auditoriaService.RegistrarAsync("DesactivarBeneficio", ActorTipo.Admin, null, "Beneficio", id);
        return MapToResponseDTO(beneficio);
    }

    private static BeneficioResponseDTO MapToResponseDTO(Beneficio b) => new()
    {
        Id = b.Id,
        Nombre = b.Nombre,
        Descripcion = b.Descripcion,
        CostoPuntos = b.CostoPuntos,
        Categoria = b.Categoria,
        Activo = b.Activo
    };
}
