using FidelixAPI.Shared.DTOs.Reporte;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Reportes para el admin (CU-24).</summary>
public interface IReporteService
{
    /// <summary>Genera el reporte pedido para el período, calculado sobre los movimientos registrados (RN-19).</summary>
    Task<ReporteResponseDTO> GenerarReporteAsync(ReporteQueryDTO query);

    /// <summary>Compara indicadores del dashboard (Ventas, Canjes, Puntos otorgados) día por día, en uno o dos períodos.</summary>
    Task<ComparacionResponseDTO> CompararAsync(ComparacionQueryDTO query);
}
