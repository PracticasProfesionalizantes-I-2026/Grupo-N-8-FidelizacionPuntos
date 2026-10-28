using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Cliente del programa de fidelización (CU-01). El documento y el email son
/// únicos (RN-01); la baja es siempre lógica (`Activo = false`, RN-14),
/// preservando el historial de movimientos.
/// </summary>
public class Cliente : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contraseña (nunca se guarda la contraseña en texto plano).</summary>
    public string? PasswordHash { get; set; }

    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;

    /// <summary>Categoría del cliente: define el multiplicador al acumular puntos (N1: Gold x1.5, CU-10).</summary>
    public CategoriaCliente Categoria { get; set; } = CategoriaCliente.Estandar;

    /// <summary>Cantidad de intentos fallidos consecutivos de login (RN-03). Vuelve a 0 con un login exitoso.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Momento en el que finaliza el bloqueo temporal (la hora está en UTC).</summary>
    public DateTime? BloqueadoHasta { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
