using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Acceso a datos de <see cref="Admin"/> (CU-14, CU-25). No extiende
/// <see cref="IRepositorioBase{TEntity}"/> a propósito: no hay ABM de Admin
/// (se siembra vía `DbInitializer`), así que forzar `CreateAsync`/`GetAllAsync`
/// en la interfaz sería exponer operaciones que ningún service va a usar
/// (ISP). `UpdateAsync` sí se necesita: el login (RN-03) y la recuperación de
/// contraseña (CU-25) mutan estado del admin aunque no exista un ABM completo.
/// </summary>
public interface IAdminRepository
{
    /// <summary>Con tracking: el llamador puede modificarlo y guardar con <see cref="UpdateAsync"/>.</summary>
    Task<Admin?> GetByIdAsync(Guid id);

    /// <summary>Busca por email (login CU-14), con tracking.</summary>
    Task<Admin?> GetByEmailAsync(string email);

    /// <summary>
    /// El primer admin activo registrado, en modo solo lectura. El MVP asume
    /// un único admin; RN-28 no especifica cómo elegir entre varios, así que
    /// se documenta esta simplificación explícitamente en vez de adivinar un
    /// criterio.
    /// </summary>
    Task<Admin?> GetPrimerActivoAsync();

    Task UpdateAsync(Admin entity);
}
