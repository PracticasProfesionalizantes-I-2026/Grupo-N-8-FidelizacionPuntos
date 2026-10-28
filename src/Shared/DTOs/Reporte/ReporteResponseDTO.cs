namespace FidelixAPI.Shared.DTOs.Reporte;

/// <summary>
/// Resultado de un reporte (CU-24). Se calcula sobre los mismos movimientos inmutables que se
/// auditan, por eso es consistente con la auditoría (RN-19).
/// </summary>
public class ReporteResponseDTO
{
    public string Tipo { get; set; } = string.Empty;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    /// <summary>"No hay datos disponibles para el período seleccionado" si el período está vacío (flujo 4a).</summary>
    public string? Mensaje { get; set; }

    public int CantidadMovimientos { get; set; }
    public int ClientesConMovimientos { get; set; }
    public int PuntosAcumulados { get; set; }
    public int PuntosBonoCumpleanos { get; set; }
    public int PuntosCanjeados { get; set; }
    public int PuntosVencidos { get; set; }

    /// <summary>Registros de auditoría del mismo período, para poder contrastar (RN-19).</summary>
    public int RegistrosAuditoria { get; set; }

    /// <summary>Solo en el reporte BeneficiosMasCanjeados.</summary>
    public List<RankingBeneficioDTO> Ranking { get; set; } = [];
}

/// <summary>Una fila del ranking de beneficios más canjeados.</summary>
public class RankingBeneficioDTO
{
    public Guid BeneficioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadCanjes { get; set; }
    public int PuntosCanjeados { get; set; }
}
