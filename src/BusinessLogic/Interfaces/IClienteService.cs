using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Ciclo de vida del cliente desde su propia perspectiva (CU-01, CU-04).</summary>
public interface IClienteService
{
    /// <summary>Autorregistro (CU-01): requiere contraseña propia y documento/email únicos.</summary>
    Task<ClienteResponseDTO> RegistrarAsync(ClienteCreateDTO dto);

    /// <summary>Saldo disponible del cliente (CU-04): suma de lotes vigentes, ya excluidos los vencidos (RN-05).</summary>
    Task<SaldoResponseDTO> ConsultarSaldoAsync(Guid clienteId);
}
