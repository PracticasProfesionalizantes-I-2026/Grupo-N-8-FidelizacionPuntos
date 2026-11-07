namespace FidelixAPI.API.IntegrationTests;

/// <summary>
/// Ids enteros para los tests de integración. Con ids autoincrementales ya no sirve `Guid.NewGuid()`
/// para "un id que no existe": hay que usar un número que la base nunca vaya a generar.
/// </summary>
public static class IdsDePrueba
{
    /// <summary>
    /// Id que nunca existe (la base numera desde 1 y no llega a este valor). Es un entero válido, así
    /// que la ruta `{id:int}` coincide y el 404 lo decide el servicio, que es lo que se quiere probar.
    /// </summary>
    public const int Inexistente = int.MaxValue;

    /// <summary>Último id entregado por <see cref="Nuevo"/>; arranca alto para no chocar con los de la base.</summary>
    private static int _ultimo = 1_000_000;

    /// <summary>Devuelve un id único por llamada (seguro entre tests en paralelo), lejos de los que genera la base.</summary>
    public static int Nuevo() => Interlocked.Increment(ref _ultimo);
}
