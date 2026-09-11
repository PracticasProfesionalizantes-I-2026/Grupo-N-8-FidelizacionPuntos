using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Empleado;

/// <summary>Modificación de datos de un empleado por el admin (CU-16).</summary>
public class EmpleadoUpdateDTO
{
    [MaxLength(100)]
    public string? Nombre { get; set; }

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }
}
