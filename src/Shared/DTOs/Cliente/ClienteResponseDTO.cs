namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// Representación pública de un cliente devuelta por la API. Nunca expone
/// <c>PasswordHash</c>.
/// </summary>
public class ClienteResponseDTO
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}
