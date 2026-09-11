namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Código temporal de recuperación de contraseña (CU-25). Exactamente una de
/// las tres referencias (Cliente/Empleado/Admin) es no-nula, según quién lo
/// solicitó. El código se guarda hasheado, igual que una contraseña.
/// </summary>
public class CodigoRecuperacion : EntidadBase
{
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public Guid? AdminId { get; set; }
    public Admin? Admin { get; set; }

    public string CodigoHash { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Vencimiento corto por diseño (RN-27).</summary>
    public DateTime FechaExpiracion { get; set; }

    public bool Usado { get; set; }
}
