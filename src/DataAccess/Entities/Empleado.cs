namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Empleado del local, gestionado por el admin (CU-16). Inicia sesión con CU-09, registra
/// acumulaciones (CU-10) y canjes presenciales (CU-11). Documento y email únicos (RN-01);
/// la baja es lógica y le quita el acceso (RN-15).
/// </summary>
public class Empleado : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contraseña (nunca se guarda en texto plano).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
}
