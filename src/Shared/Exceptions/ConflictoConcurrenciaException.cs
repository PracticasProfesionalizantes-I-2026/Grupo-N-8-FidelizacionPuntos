namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Otra operación modificó los mismos datos entre que se leyeron y se guardaron (por ejemplo, dos
/// canjes simultáneos del mismo cliente sobre los mismos lotes de puntos). La operación se revierte
/// entera y se mapea a 409 Conflict: el cliente puede reintentar y el saldo se vuelve a validar (CRIT-01).
/// La lanza la capa de datos en lugar de la excepción de EF Core, para que BusinessLogic no dependa de EF.
/// </summary>
public class ConflictoConcurrenciaException(string message, Exception? innerException = null)
    : Exception(message, innerException);
