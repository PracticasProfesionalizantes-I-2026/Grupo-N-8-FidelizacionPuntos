namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Falla no controlada de la Capa de Persistencia (ej. el repositorio no
/// pudo guardar o recuperar datos). Se mapea a 500 Internal Server Error.
/// </summary>
public class PersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
