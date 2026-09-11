namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>Representación pública de un canje realizado (CU-07, CU-08, CU-11, CU-13).</summary>
public class CanjeResponseDTO
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid BeneficioId { get; set; }
    public int PuntosUtilizados { get; set; }
    public DateTime Fecha { get; set; }
    public Guid? EmpleadoId { get; set; }
}
