namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// Resultado reducido de la búsqueda por DNI: lo mínimo que el empleado necesita para operar con el
/// cliente (su id y confirmar que es la persona correcta). No expone datos de contacto (email, teléfono).
/// </summary>
public class ClienteBusquedaResponseDTO
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;

    /// <summary>"Estandar" o "Gold" (N1: Gold acumula x1.5).</summary>
    public string Categoria { get; set; } = string.Empty;
}
