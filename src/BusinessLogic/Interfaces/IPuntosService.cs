using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Motor de puntos: saldo disponible (CU-04), consumo FIFO compartido por los
/// canjes (RN-09, CU-23) y los dos procesos batch de Sistema (CU-22, CU-26).
/// </summary>
public interface IPuntosService
{
    /// <summary>Saldo disponible de un cliente: suma de lotes vigentes, ya excluidos los vencidos (RN-05).</summary>
    Task<SaldoResponseDTO> ConsultarSaldoAsync(Guid clienteId);

    /// <summary>
    /// Descuenta `cantidad` puntos del cliente consumiendo sus lotes del más
    /// antiguo al más nuevo (RN-09). Usado por `CanjeService` al confirmar un
    /// canje (CU-07/CU-11, CU-23). Lanza `SaldoInsuficienteException` (RN-08)
    /// si el saldo total no alcanza, sin mutar nada.
    /// </summary>
    Task DescontarPuntosFifoAsync(Guid clienteId, int cantidad);

    /// <summary>Job diario: da de baja los lotes ya vencidos (CU-22).</summary>
    Task AplicarVencimientoAsync();

    /// <summary>Job diario: acredita el bono a los clientes que cumplen años hoy (CU-26, RN-29).</summary>
    Task AplicarBonoCumpleanosAsync();
}
