namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La contraseña ingresada no cumple la política mínima del sistema (RN-02).
/// Se mapea a 400 Bad Request.
/// </summary>
public class PasswordInvalidaException(string message) : Exception(message);
