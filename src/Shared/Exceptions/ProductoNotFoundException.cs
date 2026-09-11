namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No existe el producto solicitado. Se mapea a 404 Not Found.
/// </summary>
public class ProductoNotFoundException(string message) : Exception(message);
