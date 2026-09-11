using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Reporte;

/// <summary>Reporte agregado para un tipo y período dados (CU-24). Nunca modifica datos de origen.</summary>
public class ReporteResponseDTO
{
    public TipoReporte Tipo { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    /// <summary>Cantidad de registros que componen el reporte (movimientos o clientes, según el tipo).</summary>
    public int TotalRegistros { get; set; }

    /// <summary>Suma de puntos del período. Null cuando el tipo de reporte no es de puntos (ej. ClientesActivos).</summary>
    public int? TotalPuntos { get; set; }

    /// <summary>Mensaje informativo cuando no hay datos para el período (4a); null en el caso normal.</summary>
    public string? Mensaje { get; set; }
}
