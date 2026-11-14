using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="CodigoRecuperacion"/> (CU-25).</summary>
public interface ICodigoRecuperacionRepository
{
    /// <summary>Genera el Id y persiste un código nuevo.</summary>
    Task<CodigoRecuperacion> CreateAsync(CodigoRecuperacion entity);

    /// <summary>Códigos sin usar y sin vencer de un email, con tracking (al confirmar se marcan como usados).</summary>
    Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorEmailAsync(string email, DateTime ahora);

    /// <summary>
    /// Persiste los cambios sobre varios códigos ya trackeados (se marcan como usados todos los vigentes
    /// de un email, o se les suma un intento fallido). La firma deja explícito que se guarda un conjunto.
    /// </summary>
    Task UpdateRangoAsync(IEnumerable<CodigoRecuperacion> codigos);
}
