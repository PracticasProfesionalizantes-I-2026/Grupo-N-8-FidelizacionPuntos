namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>Representación pública de una regla de acumulación.</summary>
public class ReglaAcumulacionResponseDTO
{
    public Guid Id { get; set; }
    public decimal PuntosPorMonto { get; set; }
    public DateTime VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public bool Activa { get; set; }
    public int DiasVigenciaPuntos { get; set; }
}
