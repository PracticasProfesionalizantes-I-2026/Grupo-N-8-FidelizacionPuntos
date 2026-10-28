namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No hay ninguna regla de acumulación vigente para calcular los puntos (precondición de CU-10). Se mapea 409 Conflict.
/// </summary>
public class ReglaAcumulacionNoVigenteException(string message) : Exception(message);
