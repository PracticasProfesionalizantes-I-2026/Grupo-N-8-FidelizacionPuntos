namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Administrador del sistema (CU-14). Se crea desde el seed: no hay un CU para darlo de alta.
/// Accede a los módulos de configuración (RN-11) y recibe los códigos de recuperación de
/// contraseña de los empleados (RN-28).
/// </summary>
public class Admin : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contraseña (nunca se guarda en texto plano).</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
