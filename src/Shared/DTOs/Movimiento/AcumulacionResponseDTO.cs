namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>Confirmación de la acumulación (CU-10): cuánto se acreditó, cómo se calculó y el saldo del cliente.</summary>
public class AcumulacionResponseDTO
{
    public Guid MovimientoId { get; set; }
    public Guid ClienteId { get; set; }

    /// <summary>Total de la compra, calculado con los precios de los productos.</summary>
    public decimal MontoTotal { get; set; }

    /// <summary>Puntos por $1 de la regla vigente que se aplicó (RN-12).</summary>
    public decimal PuntosPorMonto { get; set; }

    /// <summary>1 para Estándar, 1.5 para Gold (N1).</summary>
    public decimal Multiplicador { get; set; }

    public int PuntosAcreditados { get; set; }

    /// <summary>Desde cuándo se pueden canjear estos puntos (N2: quedan pendientes 15 días).</summary>
    public DateTime FechaDisponible { get; set; }

    public DateTime FechaVencimiento { get; set; }

    /// <summary>Saldo canjeable hoy: no incluye los puntos todavía pendientes.</summary>
    public int SaldoDisponible { get; set; }
}
