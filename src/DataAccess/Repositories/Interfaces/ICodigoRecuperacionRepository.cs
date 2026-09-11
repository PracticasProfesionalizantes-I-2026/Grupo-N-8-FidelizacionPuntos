using FidelixAPI.DataAccess.Entities;

namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>Acceso a datos de <see cref="CodigoRecuperacion"/> (CU-25).</summary>
public interface ICodigoRecuperacionRepository : IRepositorioBase<CodigoRecuperacion>
{
    /// <summary>
    /// Códigos vigentes (no usados, no vencidos) emitidos para una cuenta
    /// puntual, para verificar el ingresado en la confirmación (RN-27). Se
    /// filtra por cuenta y no por el código en texto plano, porque el código
    /// se guarda hasheado (no es buscable por igualdad directa en la DB).
    /// </summary>
    Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorClienteAsync(Guid clienteId, DateTime fechaReferencia);
    Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorEmpleadoAsync(Guid empleadoId, DateTime fechaReferencia);
    Task<IReadOnlyList<CodigoRecuperacion>> GetVigentesPorAdminAsync(Guid adminId, DateTime fechaReferencia);
}
