using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// El cliente modifica su propio perfil (CU-03). Actualización parcial: solo cambian los campos
/// enviados. <see cref="Documento"/> existe solo para detectar el intento de cambiarlo (RN-04).
/// </summary>
public class ClienteUpdateDTO
{
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")]
    public string? Nombre { get; set; }

    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    /// <summary>Nueva contraseña (opcional). Se valida con la política RN-02 en el service.</summary>
    public string? Password { get; set; }

    /// <summary>Si viene y es distinto al actual, se rechaza (RN-04).</summary>
    public string? Documento { get; set; }
}
