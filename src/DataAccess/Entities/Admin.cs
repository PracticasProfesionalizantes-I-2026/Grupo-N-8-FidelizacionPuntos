namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Administrador del sistema (CU-14). No tiene ABM propio: se carga vía
/// `DbInitializer` como dato de arranque. Es además el destino del código de
/// recuperación de contraseña de los empleados (RN-28).
/// </summary>
public class Admin : CuentaConCredenciales
{
    public string Nombre { get; set; } = string.Empty;

    public ICollection<CodigoRecuperacion> CodigosRecuperacion { get; set; } = [];
}
