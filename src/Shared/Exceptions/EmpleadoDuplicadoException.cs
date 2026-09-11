namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El documento o email de un empleado ya está registrado (RN-01). Se mapea
/// a 409 Conflict.
/// </summary>
public class EmpleadoDuplicadoException(string message) : Exception(message);
