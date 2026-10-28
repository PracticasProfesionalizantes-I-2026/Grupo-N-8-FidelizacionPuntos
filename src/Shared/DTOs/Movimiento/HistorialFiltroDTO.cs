using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Filtros opcionales del historial del cliente (CU-05), recibidos como query params. Un valor
/// con formato inválido (ej. `fechaDesde=hola`) o un rango invertido devuelven 400 (flujo 2a).
/// </summary>
public class HistorialFiltroDTO : IValidatableObject
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public TipoMovimiento? TipoMovimiento { get; set; }

    /// <summary>Rechaza un rango de fechas invertido.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaDesde is not null && FechaHasta is not null && FechaDesde > FechaHasta)
        {
            yield return new ValidationResult("fechaDesde no puede ser posterior a fechaHasta.", [nameof(FechaDesde)]);
        }
    }
}
