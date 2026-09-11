using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Operaciones comunes a los repositorios de entidades con `Id` propio. No
/// incluye un `DeleteAsync` físico a propósito: en todo el dominio Fidelix la
/// baja siempre es lógica (`Activo = false` vía `UpdateAsync`), así que
/// exponer un borrado físico sería una capacidad muerta que nadie usa (YAGNI)
/// y una tentación de romper RN-14/RN-15/RN-17/RN-26.
/// </summary>
public interface IRepositorioBase<TEntity> where TEntity : EntidadBase
{
    /// <summary>Busca por Id. Devuelve `null` si no existe (no lanza NotFound: eso lo decide el service).</summary>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>Lista todas las entidades, en modo solo lectura (`AsNoTracking`).</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync();

    /// <summary>Genera el Id (GUID) y persiste una entidad nueva.</summary>
    Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>Persiste cambios sobre una entidad existente (incluye bajas lógicas).</summary>
    Task UpdateAsync(TEntity entity);
}
