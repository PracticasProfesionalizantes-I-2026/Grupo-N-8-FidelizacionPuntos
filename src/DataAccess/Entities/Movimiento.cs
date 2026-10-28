using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Movimiento de puntos de un cliente (acumulación, canje, bono o
/// vencimiento). Se modela como una única tabla distinguida por
/// <see cref="Tipo"/> en vez de una tabla por tipo, porque todos comparten el
/// mismo ciclo de vida de saldo y vencimiento (RN-05, RN-06).
/// </summary>
public class Movimiento : EntidadBase
{
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Monto original del movimiento: positivo en acumulación/bono; negativo en canje/vencimiento (histórico, nunca cambia).</summary>
    public int Puntos { get; set; }

    /// <summary>
    /// Solo aplica a lotes (`Acumulacion`/`BonoCumpleanos`): cuánto de
    /// <see cref="Puntos"/> queda todavía sin consumir ni vencer. Arranca
    /// igual a <see cref="Puntos"/>; es lo que permite calcular el saldo
    /// disponible (RN-05) sin recorrer los canjes posteriores.
    /// </summary>
    public int PuntosDisponibles { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de vencimiento del lote (solo en `Acumulacion` y `BonoCumpleanos`, RN-21).</summary>
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>
    /// Desde cuándo el lote se puede canjear (N2: los puntos acumulados quedan "Pendientes"
    /// 15 días, CU-10). Mientras no llegue esa fecha el lote no suma al saldo disponible ni
    /// participa del FIFO (CU-04, CU-07). `null` = disponible desde que se creó.
    /// </summary>
    public DateTime? FechaDisponible { get; set; }

    /// <summary>Beneficio canjeado (solo en movimientos de tipo `Canje`).</summary>
    public Guid? BeneficioId { get; set; }
    public Beneficio? Beneficio { get; set; }

    public string? Detalle { get; set; }

    /// <summary>Estado de entrega (solo en movimientos de tipo `Canje`, RN-10).</summary>
    public EstadoCanje? EstadoCanje { get; set; }

    /// <summary>Empleado que registró el movimiento (acumulación o canje presencial), para auditoría (RN-13, RF-28).</summary>
    public Guid? EmpleadoId { get; set; }
}
