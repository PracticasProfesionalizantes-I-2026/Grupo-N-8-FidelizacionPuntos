using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Representación pública de un movimiento de puntos (acumulación, canje,
/// bono de cumpleaños o vencimiento). Usada en el historial del cliente
/// (CU-05) y en la consulta admin (CU-20).
/// </summary>
public class MovimientoResponseDTO
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Puntos { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public Guid? EmpleadoId { get; set; }
    public string? Detalle { get; set; }
}
