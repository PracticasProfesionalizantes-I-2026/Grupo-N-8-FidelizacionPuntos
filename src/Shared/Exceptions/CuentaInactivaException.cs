namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La cuenta está inactiva. Se mapea a 403 Forbidden (RN-14).
/// </summary>
public class CuentaInactivaException(string message) : Exception(message);
