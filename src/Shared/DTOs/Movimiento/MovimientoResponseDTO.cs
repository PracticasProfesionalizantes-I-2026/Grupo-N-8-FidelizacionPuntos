namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>
/// Un movimiento de puntos en el historial (CU-05) o en la consulta del admin (CU-20). Los
/// enums viajan como texto ("Acumulacion", "Canje"...) para que se lean sin conocer su número.
/// </summary>
public class MovimientoResponseDTO
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Puntos { get; set; }
    public int PuntosDisponibles { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>Solo en lotes: desde cuándo se pueden canjear (N2).</summary>
    public DateTime? FechaDisponible { get; set; }

    /// <summary>Solo en lotes: cuándo vencen (RN-21).</summary>
    public DateTime? FechaVencimiento { get; set; }

    public string? Detalle { get; set; }
    public Guid? BeneficioId { get; set; }
    public string? BeneficioNombre { get; set; }

    /// <summary>Solo en canjes: "Pendiente" o "Entregado" (RN-10).</summary>
    public string? EstadoCanje { get; set; }

    /// <summary>Empleado que registró el movimiento, si corresponde (RF-28).</summary>
    public Guid? EmpleadoId { get; set; }
}
