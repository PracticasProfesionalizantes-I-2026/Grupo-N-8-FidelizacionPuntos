namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Producto gestionado por el admin (CU-18). El
/// nombre es unico (RN-24), el precio del producto tiene que ser mayor a 0 (RN-25);
/// la desactivacion es logica y no aparece para registrar una compra (RN-26).
/// </summary>
public class Producto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
}

