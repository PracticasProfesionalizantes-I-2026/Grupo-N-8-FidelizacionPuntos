using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Empleado;

/// <summary>El admin modifica un empleado (CU-16). Actualización parcial; el documento no se modifica (RN-04).</summary>
public class EmpleadoUpdateDTO
{
    [MaxLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")]
    public string? Nombre { get; set; }

    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Nueva contraseña (opcional), validada con la política RN-02.</summary>
    public string? Password { get; set; }
}
