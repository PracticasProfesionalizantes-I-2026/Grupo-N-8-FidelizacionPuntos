namespace FidelixAPI.Shared.DTOs.Canje;

/// <summary>Un canje en el listado del cliente (CU-08) o del empleado (CU-13), con su estado (RN-10).</summary>
public class CanjeItemDTO
{
    public Guid Id { get; set; }
    public Guid? BeneficioId { get; set; }
    public string BeneficioNombre { get; set; } = string.Empty;
    public int PuntosDescontados { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
}
