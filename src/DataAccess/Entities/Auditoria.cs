namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Registro de una operación crítica (RN-13), consultable por el admin (CU-21). Es inmutable
/// (RN-20): su repositorio solo permite crear y consultar, nunca modificar ni borrar.
/// </summary>
public class Auditoria : EntidadBase
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Qué se hizo, como "Controller.Accion" (ej.: "AdminProductos.CrearProducto") o el nombre del proceso automático.</summary>
    public string TipoOperacion { get; set; } = string.Empty;

    /// <summary>Quién lo hizo (id del token); `null` en operaciones anónimas o del sistema.</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Rol del actor ("Cliente", "Empleado", "Admin" o "Sistema").</summary>
    public string ActorRol { get; set; } = string.Empty;

    /// <summary>Id de la entidad afectada, si se puede identificar.</summary>
    public Guid? EntidadId { get; set; }

    public string Detalle { get; set; } = string.Empty;
}
