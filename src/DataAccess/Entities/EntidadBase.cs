namespace FidelixAPI.DataAccess.Entities;

/// <summary>
/// Base común para todas las entidades persistidas: todas usan un `int`
/// autoincremental como identificador (observación del profesor: ids cortos y
/// legibles en vez de `Guid`). Lo genera la base de datos al insertar
/// (`INTEGER PRIMARY KEY AUTOINCREMENT` en SQLite, por convención de EF Core);
/// el repositorio no lo asigna y EF lo carga en la entidad después de `SaveChanges`.
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }
}
