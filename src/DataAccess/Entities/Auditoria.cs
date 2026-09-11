using FidelixAPI.Shared.Enums;

namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Registro inmutable de una operación relevante del sistema (CU-21): ABM de
/// clientes/empleados/beneficios/productos, cambios de reglas de
/// acumulación, acumulaciones, canjes, vencimientos y bonos de cumpleaños
/// (RN-13, RN-19). Es de solo inserción (RN-20): el repositorio no expone
/// `UpdateAsync` ni `DeleteAsync` para esta entidad.
///
/// <see cref="ActorId"/> y <see cref="EntidadId"/> son referencias lógicas,
/// sin FK ni navegación EF: el log de auditoría no debe acoplarse al ciclo de
/// vida de la entidad auditada (que puede ser de distintos tipos según
/// <see cref="EntidadAfectada"/>).
/// </summary>
public class Auditoria : EntidadBase
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Ej.: "CrearCliente", "ConfigurarReglaAcumulacion", "AplicarVencimiento".</summary>
    public string TipoOperacion { get; set; } = string.Empty;

    public ActorTipo ActorTipo { get; set; }

    /// <summary>Null cuando <see cref="ActorTipo"/> es <c>Sistema</c> (CU-22, CU-26).</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Ej.: "Cliente", "Beneficio", "ReglaAcumulacion".</summary>
    public string EntidadAfectada { get; set; } = string.Empty;

    public Guid? EntidadId { get; set; }

    public string? Detalle { get; set; }
}
