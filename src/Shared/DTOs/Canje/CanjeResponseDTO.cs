namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>Confirmación del canje (CU-07).</summary>
public class CanjeResponseDTO
{
    public Guid Id { get; set; }
    public Guid BeneficioId { get; set; }
    public string BeneficioNombre { get; set; } = string.Empty;
    public int PuntosDescontados { get; set; }
    public int SaldoRestante { get; set; }

    /// <summary>"Pendiente" hasta que el empleado entregue el beneficio (CU-11, versión futura).</summary>
    public string Estado { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }
}
