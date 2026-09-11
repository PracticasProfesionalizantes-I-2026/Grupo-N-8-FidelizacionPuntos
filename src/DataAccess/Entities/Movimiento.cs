using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Movimiento de puntos de un cliente: acumulación (CU-10), canje (CU-07,
/// CU-11), bono de cumpleaños (CU-26) o vencimiento (CU-22). Se modela como
/// una única tabla distinguida por <see cref="Tipo"/> en vez de una tabla
/// por tipo, porque todos comparten el mismo ciclo de vida de saldo,
/// vencimiento y auditoría (RN-05, RN-06, RN-13) y separarlos duplicaría esa
/// lógica en cuatro lugares.
/// </summary>
public class Movimiento : EntidadBase
{
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Positivo en acumulación/bono; negativo en canje/vencimiento.</summary>
    public int Puntos { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de vencimiento del lote (solo en `Acumulacion` y
    /// `BonoCumpleanos`); se usa para aplicar RN-05/RN-09/RN-21 (FIFO y
    /// exclusión de lotes vencidos del saldo disponible).
    /// </summary>
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>
    /// Empleado que registró el movimiento (RF-28, trazabilidad). Queda
    /// `null` únicamente en los procesos batch de Sistema (CU-22, CU-26); es
    /// obligatorio en CU-10 y CU-11.
    /// </summary>
    public Guid? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    /// <summary>Beneficio canjeado (solo en movimientos de tipo `Canje`).</summary>
    public Guid? BeneficioId { get; set; }
    public Beneficio? Beneficio { get; set; }

    public string? Detalle { get; set; }
}
