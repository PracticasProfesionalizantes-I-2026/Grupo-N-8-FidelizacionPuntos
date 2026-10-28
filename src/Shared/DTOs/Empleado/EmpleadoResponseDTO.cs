namespace FidelixAPI.Shared.DTOs.Empleado;

/// <summary>Representación pública de un empleado (CU-16). Nunca expone el hash de la contraseña.</summary>
public class EmpleadoResponseDTO
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
}
