namespace FidelixAPI.Shared.DTOs.Reporte;

/// <summary>
/// Resultado de la comparación del dashboard (CU-24): una serie por indicador (mismo período) o una
/// por período (mismo indicador). Cada serie trae un valor por día, listo para dibujar el gráfico.
/// </summary>
public class ComparacionResponseDTO
{
    /// <summary>"No hay datos disponibles para el período seleccionado" si todas las series dan 0 (flujo 4a).</summary>
    public string? Mensaje { get; set; }

    public List<SerieComparacionDTO> Series { get; set; } = [];
}

/// <summary>Una línea del gráfico: un indicador medido día por día en un período (fechas en hora Argentina).</summary>
public class SerieComparacionDTO
{
    public string Indicador { get; set; } = string.Empty;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    /// <summary>Suma de todos los días de la serie.</summary>
    public int Total { get; set; }

    /// <summary>Un punto por cada día del período, incluidos los días en 0 (para que el gráfico no tenga huecos).</summary>
    public List<PuntoSerieDTO> Valores { get; set; } = [];
}

/// <summary>Valor de un indicador en un día (hora Argentina).</summary>
public class PuntoSerieDTO
{
    public DateOnly Fecha { get; set; }
    public int Valor { get; set; }
}
