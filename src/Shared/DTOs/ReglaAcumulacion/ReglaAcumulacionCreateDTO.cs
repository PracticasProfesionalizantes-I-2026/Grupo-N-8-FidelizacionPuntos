using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>
/// El admin da de alta una versión de la regla de acumulación (CU-19). Los campos son
/// nullable a propósito: así `[Required]` detecta cuando no vienen en el JSON (flujo 3b).
/// Con un `decimal` o `int` común, un campo faltante llegaría como 0 y pasaría el `[Required]`.
/// </summary>
public class ReglaAcumulacionCreateDTO
{
    [Required(ErrorMessage = "Campos sin rellenar: puntosPorMonto es obligatorio.")]
    public decimal? PuntosPorMonto { get; set; }

    [Required(ErrorMessage = "Campos sin rellenar: vigenciaDesde es obligatorio.")]
    public DateTime? VigenciaDesde { get; set; }

    [Required(ErrorMessage = "Campos sin rellenar: diasVigenciaPuntos es obligatorio.")]
    public int? DiasVigenciaPuntos { get; set; }
}
