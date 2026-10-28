using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Reporte;

/// <summary>
/// Parámetros del reporte (CU-24), recibidos como query params. Un tipo inexistente, un
/// parámetro faltante o un período invertido devuelven 400 antes de llegar al service (flujo 2a).
/// </summary>
public class ReporteQueryDTO : IValidatableObject
{
    [Required(ErrorMessage = "El tipo de reporte es obligatorio.")]
    public TipoReporte? Tipo { get; set; }

    [Required(ErrorMessage = "periodoDesde es obligatorio.")]
    public DateTime? PeriodoDesde { get; set; }

    [Required(ErrorMessage = "periodoHasta es obligatorio.")]
    public DateTime? PeriodoHasta { get; set; }

    /// <summary>Rechaza un período con inicio posterior al fin.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PeriodoDesde is not null && PeriodoHasta is not null && PeriodoDesde > PeriodoHasta)
        {
            yield return new ValidationResult("periodoDesde no puede ser posterior a periodoHasta.", [nameof(PeriodoDesde)]);
        }
    }
}
