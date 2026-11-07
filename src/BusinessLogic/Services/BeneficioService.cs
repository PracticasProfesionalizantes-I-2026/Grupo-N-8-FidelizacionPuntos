using System.ComponentModel;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Beneficio;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IBeneficioService"/> (CU-06, CU-17).</summary>
public class BeneficioService(IBeneficioRepository beneficioRepository) : IBeneficioService
{
    /// <summary>Costo máximo de un beneficio: hasta 6 dígitos enteros (RN-16).</summary>
    private const int CostoMaximo = 999_999;

    /// <summary>Alta: nombre único (RN-23) y costo en puntos válido (RN-16).</summary>
    public async Task<BeneficioResponseDTO> CrearAsync(BeneficioCreateDTO dto)
    {
        ValidarCosto(dto.CostoPuntos);

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

        return MapToResponseDTO(beneficio);
    }

    /// <summary>Modifica solo los campos presentes; valida el costo (RN-16) y nombre único si cambió (RN-23).</summary>
    public async Task<BeneficioResponseDTO> ActualizarAsync(int id, BeneficioUpdateDTO dto)
    {
        var beneficio = await beneficioRepository.GetByIdAsync(id)
            ?? throw new BeneficioNotFoundException("No se encontró el beneficio.");

        if (dto.CostoPuntos is not null)
        {
            ValidarCosto(dto.CostoPuntos.Value);
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
        return MapToResponseDTO(beneficio);
    }

    /// <summary>Desactivación lógica (RN-17): a partir de ahora, no se puede canjear.</summary>
    public async Task<BeneficioResponseDTO> DesactivarAsync(int id)
    {
        var beneficio = await beneficioRepository.GetByIdAsync(id)
            ?? throw new BeneficioNotFoundException("No se encontró el beneficio.");

        beneficio.Activo = false;
        await beneficioRepository.UpdateAsync(beneficio);
        return MapToResponseDTO(beneficio);
    }

    /// <summary>Listado del admin (CU-17): todos los beneficios, también los inactivos, ordenados por nombre; vacío no es error.</summary>
    public async Task<ListadoResponseDTO<BeneficioResponseDTO>> ListarTodosAsync()
    {
        var beneficios = await beneficioRepository.GetAllAsync();
        var items = beneficios.OrderBy(b => b.Nombre).Select(MapToResponseDTO).ToList();
        return ListadoResponseDTO<BeneficioResponseDTO>.Crear(items, "No hay beneficios registrados");
    }

    /// <summary>Catálogo del cliente: filtra los activos (RN-07) y los ordena por costo; vacío no es error (4a).</summary>
    public async Task<ListadoResponseDTO<BeneficioResponseDTO>> ListarBeneficiosAsync()
    {
        var beneficios = await beneficioRepository.GetAllAsync();
        var activos = beneficios.Where(b => b.Activo).OrderBy(b => b.CostoPuntos).Select(MapToResponseDTO).ToList();
        return ListadoResponseDTO<BeneficioResponseDTO>.Crear(activos, "No hay beneficios disponibles");
    }

    /// <summary>
    /// RN-16: el costo en puntos debe ser mayor a 0 y no superar 999.999 (6 dígitos enteros).
    /// Lo comparten el alta y la modificación. No hace falta validar decimales: es un int.
    /// </summary>
    private static void ValidarCosto(int costoPuntos)
    {
        if (costoPuntos <= 0)
        {
            throw new CostoInvalidoException("El costo en puntos debe ser mayor a cero.");
        }

        if (costoPuntos > CostoMaximo)
        {
            throw new CostoInvalidoException("El costo en puntos no puede superar 999999.");
        }
    }

    /// <summary>
    /// Categorías válidas, tomadas del enum <see cref="CategoriaBeneficio"/>: el número que se envía en
    /// `categoria` y su nombre legible (atributo `[Description]`). No consulta la base: la lista sale del
    /// código, así no puede quedar desincronizada con lo que la API acepta.
    /// </summary>
    public ListadoResponseDTO<CategoriaBeneficioDTO> ListarCategorias()
    {
        var categorias = Enum.GetValues<CategoriaBeneficio>()
            .Select(c => new CategoriaBeneficioDTO { Id = (int)c, Nombre = NombreLegible(c) })
            .ToList();
        return ListadoResponseDTO<CategoriaBeneficioDTO>.Crear(categorias, "No hay categorías definidas");
    }

    /// <summary>Nombre para mostrar de una categoría: su `[Description]`, o el nombre del enum si no tiene.</summary>
    private static string NombreLegible(CategoriaBeneficio categoria) =>
        typeof(CategoriaBeneficio).GetField(categoria.ToString())?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .FirstOrDefault()?.Description ?? categoria.ToString();

    /// <summary>Traduce la entidad al DTO público.</summary>
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
