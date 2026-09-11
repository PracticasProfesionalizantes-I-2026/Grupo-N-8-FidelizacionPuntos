using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>Alta o modificación de una regla de acumulación (CU-19).</summary>
public class ReglaAcumulacionCreateDTO
{
    [Required]
    public decimal PuntosPorMonto { get; set; }

    [Required]
    public DateTime VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }

    /// <summary>Días de vigencia de los puntos acreditados bajo esta regla (RF-22, RN-21).</summary>
    [Range(1, int.MaxValue)]
    public int DiasVigenciaPuntos { get; set; }
}
