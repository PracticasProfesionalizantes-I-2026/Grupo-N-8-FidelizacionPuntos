namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Código temporal para recuperar la contraseña (CU-25). Se guarda hasheado, vence a los pocos
/// minutos y se puede usar una sola vez (RN-27).
/// </summary>
public class CodigoRecuperacion : EntidadBase
{
    /// <summary>Email de la cuenta que pidió recuperar la contraseña.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Rol de la cuenta: indica en qué tabla está ("Cliente", "Empleado" o "Admin").</summary>
    public string Rol { get; set; } = string.Empty;

    public string CodigoHash { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool Usado { get; set; }
}
