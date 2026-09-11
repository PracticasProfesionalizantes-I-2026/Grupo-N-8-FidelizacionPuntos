namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Administrador del sistema (CU-14). No tiene ABM propio: se carga vía
/// `DbInitializer` como dato de arranque. Es además el destino del código de
/// recuperación de contraseña de los empleados (RN-28).
/// </summary>
public class Admin : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<CodigoRecuperacion> CodigosRecuperacion { get; set; } = [];
}
