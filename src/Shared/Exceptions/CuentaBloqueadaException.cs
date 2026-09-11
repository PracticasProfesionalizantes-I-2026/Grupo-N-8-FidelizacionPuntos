namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La cuenta quedó bloqueada temporalmente tras superar el umbral de intentos
/// fallidos de login (RN-03). Se mapea a 403 Forbidden.
/// </summary>
public class CuentaBloqueadaException(string message) : Exception(message);
