using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Auditoria;

/// <summary>Representación pública de un registro de auditoría (CU-21).</summary>
public class AuditoriaResponseDTO
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public ActorTipo ActorTipo { get; set; }
    public Guid? ActorId { get; set; }
    public string EntidadAfectada { get; set; } = string.Empty;
    public Guid? EntidadId { get; set; }
    public string? Detalle { get; set; }
}
