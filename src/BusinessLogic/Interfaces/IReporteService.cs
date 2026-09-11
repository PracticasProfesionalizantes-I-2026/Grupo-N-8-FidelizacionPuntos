using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Generación de reportes agregados para el admin (CU-24).</summary>
public interface IReporteService
{
    /// <summary>Genera el reporte del tipo y período indicados; nunca lanza si no hay datos (4a: reporte vacío).</summary>
    Task<ReporteResponseDTO> GenerarReporteAsync(TipoReporte tipo, DateTime periodoDesde, DateTime periodoHasta);
}
