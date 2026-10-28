using FidelixAPI.Shared.DTOs.Canje;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Canje de beneficios por el cliente (CU-07) o en el local por el empleado (CU-11).</summary>
public interface ICanjeService
{
    /// <summary>
    /// Canjea un beneficio activo (RN-17) si el cliente tiene saldo suficiente (RN-08),
    /// descontando los puntos por FIFO (RN-09) en una única transacción.
    /// </summary>
    Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto);

    /// <summary>
    /// Canje presencial (CU-11): mismas reglas, pero el cliente se identifica por documento, el
    /// beneficio se entrega en el momento y queda registrado qué empleado lo hizo (RN-13).
    /// </summary>
    Task<CanjeResponseDTO> CrearCanjePresencialAsync(CanjePresencialCreateDTO dto, Guid? empleadoId);

    /// <summary>
    /// El empleado entrega en el local un canje que el cliente hizo desde la app: pasa de "Pendiente"
    /// a "Entregado" y queda registrado qué empleado lo entregó (RN-10, RN-13).
    /// </summary>
    Task<CanjeItemDTO> MarcarEntregadoAsync(Guid canjeId, Guid? empleadoId);
}
