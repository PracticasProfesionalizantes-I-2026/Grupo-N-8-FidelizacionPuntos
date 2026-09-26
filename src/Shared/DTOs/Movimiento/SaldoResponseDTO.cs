namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>Saldo de puntos disponible de un cliente (CU-04, RN-05: excluye lotes vencidos).</summary>
public class SaldoResponseDTO
{
    public int PuntosDisponibles { get; set; }
}
