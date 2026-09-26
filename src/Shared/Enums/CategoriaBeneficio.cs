namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Clasifica un beneficio canjeable para poder agruparlos/filtrarlos en el
/// catálogo (CU-06). Los valores son un punto de partida genérico: no surgen
/// de una CU específica (ajustar según el rubro real del negocio cliente).
/// </summary>
public enum CategoriaBeneficio
{
    Descuento,
    ProductoGratis,
    Experiencia,
    Otro
}
