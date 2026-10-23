namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El id ingresado no corresponde a ningun producto. Se mapea 404 Not Found.
/// </summary>
public class ProductoNotFoundException(string message) : Exception(message);
