using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// Datos para dar de alta un cliente. Se reutiliza en los tres canales de
/// creación (CU-01 autorregistro, CU-12 alta desde POS por un empleado, CU-15
/// alta por el admin): <see cref="Password"/> es obligatorio solo en el
/// autorregistro (el cliente define su propia clave); en los otros dos
/// canales queda `null` y el cliente establece su contraseña más adelante
/// mediante recuperación de contraseña (CU-25).
/// </summary>
public class ClienteCreateDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Documento { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Obligatorio solo en el autorregistro (CU-01).</summary>
    [MinLength(8)]
    public string? Password { get; set; }

    public DateTime? FechaNacimiento { get; set; }
}
