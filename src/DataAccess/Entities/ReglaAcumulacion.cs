namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Criterio vigente para calcular puntos por compra, gestionado por el admin
/// (CU-19). Solo puede haber una versión activa a la vez (RN-18).
/// </summary>
public class ReglaAcumulacion : EntidadBase
{
    public decimal PuntosPorMonto { get; set; }
    public DateTime VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Días que tarda en vencer un lote de puntos acreditado bajo esta regla
    /// (RF-22, RN-21). RF-22 no tenía CU propio en la documentación original;
    /// se decidió extender CU-19 en vez de crear un caso de uso separado,
    /// para no tener dos pantallas de configuración de puntos distintas.
    /// </summary>
    public int DiasVigenciaPuntos { get; set; }
}
