namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El id ingresado no corresponde a ningún registro de auditoría. Se mapea 404 Not Found (CU-21).
/// </summary>
public class AuditoriaNotFoundException(string message) : Exception(message);
