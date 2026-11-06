namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Indicadores que el admin puede comparar en el dashboard (CU-24, comparación de períodos).
/// Son los mismos tres "Tipos" que muestra el prototipo: Ventas, Canjes y Puntos Otorgados.
/// </summary>
public enum IndicadorDashboard
{
    /// <summary>Cantidad de compras registradas (movimientos de acumulación, CU-10).</summary>
    Ventas,

    /// <summary>Cantidad de canjes de beneficios (CU-07, CU-11).</summary>
    Canjes,

    /// <summary>Puntos otorgados a clientes: acumulaciones por compra más bonos de cumpleaños (CU-26).</summary>
    PuntosOtorgados
}
