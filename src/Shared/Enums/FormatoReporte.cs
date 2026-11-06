namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Formato en el que el admin recibe un reporte (CU-24, sub-variación 1). El contenido es el mismo
/// en ambos casos; solo cambia la forma de entregarlo.
/// </summary>
public enum FormatoReporte
{
    /// <summary>Para ver en pantalla (opción por defecto).</summary>
    Json,

    /// <summary>Para descargar y abrir en una planilla de cálculo.</summary>
    Csv
}
