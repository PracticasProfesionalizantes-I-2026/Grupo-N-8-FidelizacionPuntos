namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Versión de la regla de acumulación de puntos, configurada por el admin (CU-19).
/// Las versiones no se pisan: cada una entra en vigor en <see cref="VigenciaDesde"/>
/// y la vigente en una fecha es la de `VigenciaDesde` más reciente que ya empezó.
/// No puede haber dos versiones con la misma fecha de inicio (RN-18). El historial
/// de versiones más las fechas de creación/modificación dejan la regla auditable (RN-13).
/// </summary>
public class ReglaAcumulacion : EntidadBase
{
    /// <summary>Puntos otorgados por cada $1 de compra (ej.: 0.1 = 1 punto cada $10).</summary>
    public decimal PuntosPorMonto { get; set; }

    /// <summary>Fecha (sin hora) desde la que esta versión pasa a ser la vigente.</summary>
    public DateTime VigenciaDesde { get; set; }

    /// <summary>Días que tarda en vencer un lote acreditado bajo esta regla (RF-22, RN-21).</summary>
    public int DiasVigenciaPuntos { get; set; }

    /// <summary>Momento en que se dio de alta la versión (RN-13).</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Momento de la última modificación; `null` si nunca se modificó (RN-13).</summary>
    public DateTime? FechaModificacion { get; set; }
}
