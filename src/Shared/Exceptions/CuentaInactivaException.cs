namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La cuenta (cliente dado de baja por RN-14, o empleado dado de baja por
/// RN-15) está inactiva e intenta autenticarse u operar. Se mapea a 403
/// Forbidden.
/// </summary>
public class CuentaInactivaException(string message) : Exception(message);
