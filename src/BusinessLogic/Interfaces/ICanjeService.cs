using FidelixAPI.Shared.DTOs.Canje;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Canje de beneficios por el cliente (CU-07).</summary>
public interface ICanjeService
{
    /// <summary>
    /// Canjea un beneficio activo (RN-17) si el cliente tiene saldo suficiente (RN-08),
    /// descontando los puntos por FIFO (RN-09) en una única transacción.
    /// </summary>
    Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto);
}
