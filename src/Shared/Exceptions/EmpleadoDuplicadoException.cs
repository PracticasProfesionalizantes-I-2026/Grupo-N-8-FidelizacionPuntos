namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Ya existe un empleado con ese documento o email (RN-01). Se mapea 409 Conflict (CU-16, flujo 3a).
/// </summary>
public class EmpleadoDuplicadoException(string message) : Exception(message);
