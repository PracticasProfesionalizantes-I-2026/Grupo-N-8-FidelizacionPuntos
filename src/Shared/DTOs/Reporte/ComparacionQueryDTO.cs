using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Reporte;

/// <summary>
/// Parámetros de la comparación del dashboard del admin (CU-24, "Tipo 1 vs Tipo 2" del prototipo),
/// recibidos como query params. Permite dos comparaciones:
/// - <b>Mismo período, distintos indicadores:</b> sin `indicador`, devuelve una serie por cada uno.
/// - <b>Mismo indicador, dos períodos:</b> con `indicador` y el segundo período.
/// Como indica la nota del prototipo, el segundo período solo se puede usar con un único indicador.
/// </summary>
public class ComparacionQueryDTO : IValidatableObject
{
    /// <summary>Máximo de días por período, para que la serie diaria del gráfico tenga un tamaño razonable.</summary>
    public const int MaximoDiasPorPeriodo = 366;

    public IndicadorDashboard? Indicador { get; set; }

    [Required(ErrorMessage = "periodo1Desde es obligatorio.")]
    public DateTime? Periodo1Desde { get; set; }

    [Required(ErrorMessage = "periodo1Hasta es obligatorio.")]
    public DateTime? Periodo1Hasta { get; set; }

    public DateTime? Periodo2Desde { get; set; }
    public DateTime? Periodo2Hasta { get; set; }

    /// <summary>Indica si se pidió el segundo período (los dos extremos informados).</summary>
    public bool TieneSegundoPeriodo() => Periodo2Desde is not null && Periodo2Hasta is not null;

    /// <summary>
    /// Reglas entre campos (flujo 2a, 400): períodos no invertidos ni demasiado largos, segundo
    /// período completo (desde y hasta) y solo junto con un indicador.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in ValidarPeriodo(Periodo1Desde, Periodo1Hasta, "periodo1"))
        {
            yield return error;
        }

        if ((Periodo2Desde is null) != (Periodo2Hasta is null))
        {
            yield return new ValidationResult("El segundo período necesita periodo2Desde y periodo2Hasta.", [nameof(Periodo2Desde)]);
        }

        if (TieneSegundoPeriodo())
        {
            if (Indicador is null)
            {
                yield return new ValidationResult("El segundo período solo se puede usar cuando se elige un indicador.", [nameof(Indicador)]);
            }

            foreach (var error in ValidarPeriodo(Periodo2Desde, Periodo2Hasta, "periodo2"))
            {
                yield return error;
            }
        }
    }

    /// <summary>Rechaza un período invertido o de más de <see cref="MaximoDiasPorPeriodo"/> días.</summary>
    private static IEnumerable<ValidationResult> ValidarPeriodo(DateTime? desde, DateTime? hasta, string nombre)
    {
        if (desde is null || hasta is null)
        {
            yield break;
        }

        if (desde > hasta)
        {
            yield return new ValidationResult($"{nombre}Desde no puede ser posterior a {nombre}Hasta.", [$"{nombre}Desde"]);
        }
        else if ((hasta.Value.Date - desde.Value.Date).TotalDays >= MaximoDiasPorPeriodo)
        {
            yield return new ValidationResult($"El {nombre} no puede superar {MaximoDiasPorPeriodo} días.", [$"{nombre}Hasta"]);
        }
    }
}
