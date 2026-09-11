namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Empleado con acceso al punto de venta (CU-09, CU-16). La baja es lógica y
/// revoca el acceso al sistema (RN-15).
/// </summary>
public class Empleado : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Null hasta que el empleado define su contraseña vía CU-25 (RN-28).</summary>
    public string? PasswordHash { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    public ICollection<Movimiento> Movimientos { get; set; } = [];
    public ICollection<CodigoRecuperacion> CodigosRecuperacion { get; set; } = [];
}
