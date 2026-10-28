using FidelixAPI.DataAccess.Entities;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Movimiento;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>
/// Traduce movimientos a DTOs. Está aparte porque lo comparten varios services (historial,
/// canjes del cliente y del empleado, consulta del admin) y así la traducción vive en un solo lugar.
/// </summary>
internal static class MovimientoMapper
{
    /// <summary> Movimiento completo para el historial (CU-05) y la consulta del admin (CU-20). </summary>
    public static MovimientoResponseDTO ToResponseDTO(Movimiento m) => new()
    {
        Id = m.Id,
        ClienteId = m.ClienteId,
        Tipo = m.Tipo.ToString(),
        Puntos = m.Puntos,
        PuntosDisponibles = m.PuntosDisponibles,
        Fecha = m.Fecha,
        FechaDisponible = m.FechaDisponible,
        FechaVencimiento = m.FechaVencimiento,
        Detalle = m.Detalle,
        BeneficioId = m.BeneficioId,
        BeneficioNombre = m.Beneficio?.Nombre,
        EstadoCanje = m.EstadoCanje?.ToString(),
        EmpleadoId = m.EmpleadoId
    };

    /// <summary> Canje resumido para los listados de canjes (CU-08, CU-13), con su estado (RN-10). </summary>
    public static CanjeItemDTO ToCanjeItemDTO(Movimiento m) => new()
    {
        Id = m.Id,
        BeneficioId = m.BeneficioId,
        BeneficioNombre = m.Beneficio?.Nombre ?? string.Empty,
        PuntosDescontados = -m.Puntos,
        Fecha = m.Fecha,
        Estado = m.EstadoCanje?.ToString() ?? string.Empty
    };
}
