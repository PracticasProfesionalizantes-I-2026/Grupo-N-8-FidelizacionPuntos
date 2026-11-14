using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FidelixAPI.DataAccess.Repositories;

/// <summary>Implementación EF Core de <see cref="IUnidadDeTrabajo"/> sobre el ChangeTracker del contexto.</summary>
public class UnidadDeTrabajo(FidelixDbContext context) : IUnidadDeTrabajo
{
    /// <summary>
    /// Recorre las entradas con cambios pendientes: las `Added` se desacoplan (no se insertarán en el
    /// próximo `SaveChanges`) y las `Modified`/`Deleted` recuperan sus valores originales y quedan
    /// `Unchanged`. A diferencia de `ChangeTracker.Clear()`, no desacopla las entidades sin cambios,
    /// así un proceso puede seguir trabajando con los lotes ya cargados de los demás clientes.
    /// </summary>
    public void DescartarCambios()
    {
        foreach (var entrada in context.ChangeTracker.Entries().ToList())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    entrada.CurrentValues.SetValues(entrada.OriginalValues);
                    entrada.State = EntityState.Unchanged;
                    break;
            }
        }
    }
}
