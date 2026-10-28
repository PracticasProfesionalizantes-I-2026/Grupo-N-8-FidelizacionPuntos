using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Empleado;

/// <summary>El admin da de alta un empleado (CU-16).</summary>
public class EmpleadoCreateDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    [MaxLength(20)]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña inicial del empleado; se valida con la política RN-02 en el service.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
