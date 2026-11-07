using System.ComponentModel;

namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Clasifica un beneficio canjeable para poder agruparlos/filtrarlos en el catálogo (CU-06). Los
/// valores son un punto de partida genérico (ajustar según el rubro del negocio). Se numeran desde 1:
/// el 0 queda sin significado, así una categoría no informada o inválida se detecta (400) en vez de
/// convertirse en una categoría real. El admin consulta la lista con `GET /api/admin/beneficios/categorias`.
/// </summary>
public enum CategoriaBeneficio
{
    /// <summary>Descuento sobre una compra.</summary>
    [Description("Descuento")]
    Descuento = 1,

    /// <summary>Un producto sin cargo.</summary>
    [Description("Producto gratis")]
    ProductoGratis = 2,

    /// <summary>Una experiencia (evento, degustación, etc.).</summary>
    [Description("Experiencia")]
    Experiencia = 3,

    /// <summary>Cualquier otro beneficio; es la categoría por defecto si no se informa.</summary>
    [Description("Otro")]
    Otro = 4
}
