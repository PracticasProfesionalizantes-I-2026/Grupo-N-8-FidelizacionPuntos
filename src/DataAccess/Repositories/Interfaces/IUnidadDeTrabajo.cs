namespace FidelixAPI.DataAccess.Repositories.Interfaces;

/// <summary>
/// Operaciones sobre el conjunto de cambios pendientes del contexto (la "unidad de trabajo" de EF
/// Core), que no pertenecen a ningún repositorio en particular. Los procesos batch (CU-22, CU-26) la
/// usan para que el error de un cliente no se arrastre al guardado del cliente siguiente (CRIT-02).
/// </summary>
public interface IUnidadDeTrabajo
{
    /// <summary>
    /// Descarta los cambios todavía no guardados: lo agregado deja de estar trackeado y lo modificado
    /// vuelve a sus valores originales. Lo ya persistido y lo no modificado no se toca.
    /// </summary>
    void DescartarCambios();
}
