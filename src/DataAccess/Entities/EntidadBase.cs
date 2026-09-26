namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Base común para todas las entidades persistidas: todas usan `Guid` como
/// identificador, asignado en el repositorio al crear el registro (nunca por
/// la base de datos), según la convención de la adenda de arquitectura.
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; set; }
}
