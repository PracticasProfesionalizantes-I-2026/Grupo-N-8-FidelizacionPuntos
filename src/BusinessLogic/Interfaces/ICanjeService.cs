using FidelixAPI.Shared.DTOs.Canje;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Canje de beneficios por puntos, propio (CU-07) y presencial (CU-11), y sus consultas (CU-08, CU-13).</summary>
public interface ICanjeService
{
    /// <summary>El propio cliente canjea un beneficio (CU-07).</summary>
    Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto);

    /// <summary>Un empleado canjea de forma presencial para un cliente puntual (CU-11).</summary>
    Task<CanjeResponseDTO> CrearCanjePresencialAsync(Guid empleadoId, CanjeCreateDTO dto);

    /// <summary>Canjes propios del cliente autenticado (CU-08).</summary>
    Task<IReadOnlyList<CanjeResponseDTO>> ConsultarPorClienteIdAsync(Guid clienteId);

    /// <summary>Canjes de un cliente puntual, consultados por un empleado (CU-13).</summary>
    Task<IReadOnlyList<CanjeResponseDTO>> ConsultarPorDocumentoAsync(string documento);
}
