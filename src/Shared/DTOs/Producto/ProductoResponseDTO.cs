namespace FidelixAPI.Shared.DTOs.Producto;

/// <summary>Representación pública de un producto.</summary>
public class ProductoResponseDTO
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}
