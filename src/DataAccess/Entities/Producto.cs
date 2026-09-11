namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Producto vendible, gestionado por el admin (CU-18). El nombre es único
/// (RN-24) y el precio debe ser positivo (RN-25); la desactivación es lógica
/// (RN-26).
/// </summary>
public class Producto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
}
