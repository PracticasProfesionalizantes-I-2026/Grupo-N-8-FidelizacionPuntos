namespace FidelixAPI.Shared.DTOs.Movimiento;

/// <summary>Filtros opcionales de la consulta de movimientos del admin (CU-20): los del historial más el cliente.</summary>
public class MovimientoFiltroDTO : HistorialFiltroDTO
{
    public Guid? ClienteId { get; set; }
}
