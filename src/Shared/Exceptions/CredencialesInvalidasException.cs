namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Credenciales inválidas. Se mapea a 401 Unauthorized (CU-02).
/// </summary>
public class CredencialesInvalidasException(string message) : Exception(message);
