namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El id ingresado no corresponde a ningún empleado. Se mapea 404 Not Found (CU-16, flujo 3b).
/// </summary>
public class EmpleadoNotFoundException(string message) : Exception(message);
