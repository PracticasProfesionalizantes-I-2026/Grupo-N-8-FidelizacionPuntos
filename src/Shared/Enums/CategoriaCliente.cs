namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Categoría del cliente en el programa de fidelización. Define el multiplicador que se
/// aplica al acumular puntos (N1 de la matriz de complejidad: Gold acumula x1.5, CU-10).
/// Se numera desde 1, igual que <see cref="CategoriaBeneficio"/>: el 0 no es una categoría válida.
/// </summary>
public enum CategoriaCliente
{
    /// <summary>Categoría por defecto: acumula con la regla vigente sin multiplicador (x1).</summary>
    Estandar = 1,

    /// <summary>Cliente preferencial: acumula con un multiplicador x1.5 (N1).</summary>
    Gold = 2
}
