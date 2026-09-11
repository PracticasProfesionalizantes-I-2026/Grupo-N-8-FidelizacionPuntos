namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El documento o email de un cliente ya está registrado (RN-01, RN-04). Se
/// mapea a 409 Conflict.
/// </summary>
public class ClienteDuplicadoException(string message) : Exception(message);
