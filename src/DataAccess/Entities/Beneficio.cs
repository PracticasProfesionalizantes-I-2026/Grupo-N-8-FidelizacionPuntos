using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Beneficio canjeable por puntos, gestionado por el admin (CU-17). El
/// nombre es único (RN-23) y el costo en puntos debe ser positivo (RN-16);
/// la desactivación es lógica y lo saca del catálogo (RN-17).
/// </summary>
public class Beneficio : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CostoPuntos { get; set; }
    public CategoriaBeneficio Categoria { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Movimiento> Canjes { get; set; } = [];
}
