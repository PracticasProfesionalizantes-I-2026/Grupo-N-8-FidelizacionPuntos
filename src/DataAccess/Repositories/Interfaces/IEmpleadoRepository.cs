using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="Empleado"/> (CU-09, CU-16).</summary>
public interface IEmpleadoRepository : IRepositorioBase<Empleado>
{
    /// <summary>Busca por documento (RN-01, unicidad).</summary>
    Task<Empleado?> GetByDocumentoAsync(string documento);

    /// <summary>Busca por email (login CU-09, recuperación CU-25, unicidad RN-01).</summary>
    Task<Empleado?> GetByEmailAsync(string email);
}
