namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>Representación pública de una versión de la regla de acumulación (CU-19).</summary>
public class ReglaAcumulacionResponseDTO
{
    public Guid Id { get; set; }
    public decimal PuntosPorMonto { get; set; }
    public DateTime VigenciaDesde { get; set; }
    public int DiasVigenciaPuntos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
