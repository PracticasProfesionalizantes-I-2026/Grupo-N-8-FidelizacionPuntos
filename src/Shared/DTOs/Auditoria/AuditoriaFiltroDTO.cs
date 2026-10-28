using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Auditoria;

/// <summary>Criterios de búsqueda de la auditoría (CU-21), recibidos como query params. Al menos uno es necesario (flujo 3a).</summary>
public class AuditoriaFiltroDTO : IValidatableObject
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    [MaxLength(100)]
    public string? TipoOperacion { get; set; }

    public Guid? ActorId { get; set; }

    /// <summary>Indica si no se informó ningún criterio (búsqueda completamente abierta).</summary>
    public bool SinCriterios() =>
        FechaDesde is null && FechaHasta is null && string.IsNullOrWhiteSpace(TipoOperacion) && ActorId is null;

    /// <summary>Rechaza un rango de fechas invertido.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaDesde is not null && FechaHasta is not null && FechaDesde > FechaHasta)
        {
            yield return new ValidationResult("fechaDesde no puede ser posterior a fechaHasta.", [nameof(FechaDesde)]);
        }
    }
}
