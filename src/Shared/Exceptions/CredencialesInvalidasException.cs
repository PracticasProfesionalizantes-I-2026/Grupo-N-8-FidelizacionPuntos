namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El email y/o la contraseña no coinciden con ninguna cuenta (Cliente,
/// Empleado o Admin) al intentar iniciar sesión. Se mapea a 401 Unauthorized.
/// </summary>
public class CredencialesInvalidasException(string message) : Exception(message);
