using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Beneficio;

/// <summary>Representación pública de un beneficio (CU-06, catálogo).</summary>
public class BeneficioResponseDTO
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CostoPuntos { get; set; }
    public CategoriaBeneficio Categoria { get; set; }
    public bool Activo { get; set; }
}
