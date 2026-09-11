namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No existe el empleado solicitado. Se mapea a 404 Not Found.
/// </summary>
public class EmpleadoNotFoundException(string message) : Exception(message);
