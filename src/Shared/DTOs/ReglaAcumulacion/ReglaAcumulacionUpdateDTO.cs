namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>Modificación de una regla de acumulación existente (CU-19).</summary>
public class ReglaAcumulacionUpdateDTO
{
    public decimal? PuntosPorMonto { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public int? DiasVigenciaPuntos { get; set; }
}
