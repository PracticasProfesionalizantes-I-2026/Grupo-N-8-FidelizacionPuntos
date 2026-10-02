namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La cuenta está bloqueada por intentos fallidos.
/// Se mapea a 403 Forbidden (RN-03).
/// </summary>
public class CuentaBloqueadaException(string message) : Exception(message);
