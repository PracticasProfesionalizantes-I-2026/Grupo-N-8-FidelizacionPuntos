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

    /// <summary>Beneficio canjeado (solo en movimientos de tipo `Canje`).</summary>
    public Guid? BeneficioId { get; set; }
    public Beneficio? Beneficio { get; set; }

    public string? Detalle { get; set; }
}
