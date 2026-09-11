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
}
