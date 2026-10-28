namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El id ingresado no corresponde a ninguna regla de acumulación. Se mapea 404 Not Found.
/// </summary>
public class ReglaAcumulacionNotFoundException(string message) : Exception(message);
