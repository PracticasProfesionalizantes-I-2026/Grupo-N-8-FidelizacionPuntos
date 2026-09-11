namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Tipos de reporte agregado que el admin puede generar (CU-24). La CU los
/// menciona a modo de ejemplo ("acumulación, canjes, clientes activos, etc.")
/// sin cerrar la lista; se implementan estos tres para el MVP.
/// </summary>
public enum TipoReporte
{
    Acumulaciones,
    Canjes,
    ClientesActivos
}
