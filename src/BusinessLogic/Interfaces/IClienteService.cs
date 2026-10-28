using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Ciclo de vida del cliente desde su propia perspectiva (CU-01, CU-03, CU-04, CU-05, CU-08) y desde el punto de venta (CU-12, CU-13).</summary>
public interface IClienteService
{
    /// <summary>Autorregistro (CU-01): requiere contraseña propia y documento/email únicos.</summary>
    Task<ClienteResponseDTO> RegistrarAsync(ClienteCreateDTO dto);

    /// <summary>Saldo disponible del cliente (CU-04): suma de lotes vigentes, ya excluidos los vencidos (RN-05).</summary>
    Task<SaldoResponseDTO> ConsultarSaldoAsync(Guid clienteId);

    /// <summary>Datos del perfil del cliente autenticado (CU-03).</summary>
    Task<ClienteResponseDTO> ObtenerPerfilAsync(Guid clienteId);

    /// <summary>Modificación parcial del perfil (CU-03): el documento no cambia (RN-04) y el email sigue siendo único (RN-01).</summary>
    Task<ClienteResponseDTO> ActualizarPerfilAsync(Guid clienteId, ClienteUpdateDTO dto);

    /// <summary>Baja voluntaria y lógica de la cuenta (CU-03, RN-14): conserva el historial.</summary>
    Task DarseDeBajaAsync(Guid clienteId);

    /// <summary>Historial de movimientos del cliente con filtros opcionales, en orden cronológico (CU-05, RN-06).</summary>
    Task<ListadoResponseDTO<MovimientoResponseDTO>> ConsultarHistorialAsync(Guid clienteId, HistorialFiltroDTO filtro);

    /// <summary>Canjes del cliente autenticado, con su estado (CU-08, RN-10).</summary>
    Task<ListadoResponseDTO<CanjeItemDTO>> ConsultarCanjesAsync(Guid clienteId);

    /// <summary>Alta de un cliente desde el punto de venta (CU-12): sin contraseña, documento/email únicos (RN-01).</summary>
    Task<ClienteResponseDTO> CrearClienteDesdePOSAsync(ClienteCreateDTO dto);

    /// <summary>Canjes de un cliente buscado por documento, para el empleado (CU-13, RN-10).</summary>
    Task<ListadoResponseDTO<CanjeItemDTO>> ConsultarCanjesPorDocumentoAsync(string documento);
}
