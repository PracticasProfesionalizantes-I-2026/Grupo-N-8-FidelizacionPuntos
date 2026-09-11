namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Clasifica el tipo de movimiento de puntos de un cliente. Todos los
/// movimientos (positivos o negativos) se guardan en una única tabla
/// `Movimientos` distinguidos por este campo, en vez de una tabla por tipo,
/// para no duplicar la lógica de saldo/vencimiento/auditoría (RN-05, RN-13).
/// </summary>
public enum TipoMovimiento
{
    /// <summary>Puntos acreditados por una compra (CU-10).</summary>
    Acumulacion,

    /// <summary>Puntos descontados por el canje de un beneficio (CU-07/CU-11).</summary>
    Canje,

    /// <summary>Puntos acreditados por el bono anual de cumpleaños (CU-26).</summary>
    BonoCumpleanos,

    /// <summary>Ajuste negativo por vencimiento de un lote de puntos (CU-22).</summary>
    Vencimiento
}
