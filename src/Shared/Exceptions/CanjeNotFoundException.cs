namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El id ingresado no corresponde a ningún canje. Se mapea 404 Not Found (RN-10).
/// </summary>
public class CanjeNotFoundException(string message) : Exception(message);
