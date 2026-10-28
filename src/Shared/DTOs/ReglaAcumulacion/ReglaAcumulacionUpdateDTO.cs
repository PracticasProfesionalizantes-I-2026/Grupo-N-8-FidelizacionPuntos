namespace FidelixAPI.Shared.DTOs.ReglaAcumulacion;

/// <summary>El admin modifica una versión de la regla (CU-19). Actualización parcial: solo cambian los campos enviados.</summary>
public class ReglaAcumulacionUpdateDTO
{
    public decimal? PuntosPorMonto { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public int? DiasVigenciaPuntos { get; set; }
}
