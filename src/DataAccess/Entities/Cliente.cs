namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Cliente del programa de fidelización (CU-01, CU-03, CU-12, CU-15). El
/// documento es inmutable (RN-04) y el email es único (RN-01); la baja es
/// siempre lógica (RN-14), preservando el historial de movimientos y canjes.
/// </summary>
public class Cliente : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Null cuando el cliente fue creado desde POS (CU-12) o por el admin
    /// (CU-15): todavía no definió una contraseña propia y debe establecerla
    /// mediante recuperación de contraseña (CU-25).
    /// </summary>
    public string? PasswordHash { get; set; }

    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Movimiento> Movimientos { get; set; } = [];
    public ICollection<CodigoRecuperacion> CodigosRecuperacion { get; set; } = [];
}
