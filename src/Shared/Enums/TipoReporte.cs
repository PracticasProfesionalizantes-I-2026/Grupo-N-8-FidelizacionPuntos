namespace FidelixAPI.Shared.Enums;

/// <summary>Reportes que puede generar el admin (CU-24).</summary>
public enum TipoReporte
{
    /// <summary>Totales del período: puntos acumulados, canjeados, vencidos y bonos.</summary>
    Resumen,

    /// <summary>Ranking de beneficios por cantidad de canjes en el período.</summary>
    BeneficiosMasCanjeados
}
