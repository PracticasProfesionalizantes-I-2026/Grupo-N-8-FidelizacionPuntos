namespace FidelixAPI.Shared.DTOs.Auditoria;

/// <summary>Un registro de auditoría (CU-21). Es de solo lectura (RN-20).</summary>
public class AuditoriaResponseDTO
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public int? ActorId { get; set; }
    public string ActorRol { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string Detalle { get; set; } = string.Empty;
}
