namespace FidelixAPI.BusinessLogic.Tests;

/// <summary>
/// Ids enteros para los tests unitarios. Reemplaza a `Guid.NewGuid()`: cada llamada a
/// <see cref="Nuevo"/> devuelve un id distinto, así dos entidades de un mismo test nunca comparten id.
/// </summary>
public static class IdsDePrueba
{
    /// <summary>Último id entregado por <see cref="Nuevo"/>.</summary>
    private static int _ultimo;

    /// <summary>Devuelve un id único por llamada (seguro entre tests en paralelo).</summary>
    public static int Nuevo() => Interlocked.Increment(ref _ultimo);
}
