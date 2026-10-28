namespace FidelixAPI.Shared.DTOs.Comun;

/// <summary>
/// Respuesta de las consultas de listado (CU-05, 06, 08, 13, 20, 21). Una lista vacía no es un
/// error (200 OK): en ese caso <see cref="Mensaje"/> explica por qué no hay resultados.
/// </summary>
public class ListadoResponseDTO<T>
{
    public string? Mensaje { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Arma la respuesta: si la lista está vacía, completa el mensaje indicado.</summary>
    public static ListadoResponseDTO<T> Crear(IReadOnlyList<T> items, string mensajeSiVacio) => new()
    {
        Items = items,
        Mensaje = items.Count == 0 ? mensajeSiVacio : null
    };
}
