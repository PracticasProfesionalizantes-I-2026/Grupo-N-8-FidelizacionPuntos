using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="ReglaAcumulacion"/> (CU-10, CU-19).</summary>
public interface IReglaAcumulacionRepository : IRepositorioBase<ReglaAcumulacion>
{
    /// <summary>Regla vigente hoy, usada para calcular puntos en CU-10.</summary>
    Task<ReglaAcumulacion?> GetActivaAsync();

    /// <summary>
    /// Reglas activas cuya vigencia se solapa con el rango dado, para validar
    /// RN-18 (una única versión activa) al crear/modificar. `idAExcluir` se
    /// usa en modificación, para no comparar la regla contra sí misma.
    /// </summary>
    Task<IReadOnlyList<ReglaAcumulacion>> GetActivasSolapadasAsync(DateTime vigenciaDesde, DateTime? vigenciaHasta, Guid? idAExcluir);
}
