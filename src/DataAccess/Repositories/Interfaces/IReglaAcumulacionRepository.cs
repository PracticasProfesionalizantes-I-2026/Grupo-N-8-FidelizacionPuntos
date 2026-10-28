using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="ReglaAcumulacion"/> (CU-19).</summary>
public interface IReglaAcumulacionRepository : IRepositorioBase<ReglaAcumulacion>
{
    /// <summary>Busca la versión que empieza exactamente en esa fecha (RN-18, superposición).</summary>
    Task<ReglaAcumulacion?> GetByVigenciaDesdeAsync(DateTime vigenciaDesde);
}
