using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// Modificación de perfil (CU-03). Todos los campos son opcionales excepto
/// que se envíen: solo se actualiza lo presente. <see cref="Documento"/> se
/// incluye únicamente para poder detectar y rechazar un intento de
/// modificarlo (RN-04, inmutable) — nunca se usa para actualizar el valor
/// almacenado.
/// </summary>
public class ClienteUpdateDTO
{
    [MaxLength(100)]
    public string? Nombre { get; set; }

    /// <summary>Presente solo para validar que coincide con el valor ya guardado (RN-04).</summary>
    public string? Documento { get; set; }

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }
}
