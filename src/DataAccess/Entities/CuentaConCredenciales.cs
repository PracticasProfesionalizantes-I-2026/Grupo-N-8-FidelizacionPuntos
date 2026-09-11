namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Base común a las tres entidades con login propio (Cliente, Empleado,
/// Admin): credenciales, estado de habilitación y bloqueo por intentos
/// fallidos (RN-03). Se extrae para no triplicar estos 4 campos y su
/// semántica idéntica en las tres entidades — <see cref="AuthService"/> los
/// trata de forma polimórfica en la cascada de login.
/// </summary>
public abstract class CuentaConCredenciales : EntidadBase
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Null cuando la cuenta todavía no definió su contraseña (ver comentario en cada entidad concreta).</summary>
    public string? PasswordHash { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Contador de logins fallidos consecutivos (RN-03); se resetea en un login exitoso.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Si tiene un valor futuro, la cuenta está bloqueada temporalmente (RN-03).</summary>
    public DateTime? BloqueadoHasta { get; set; }
}
