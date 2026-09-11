namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El precio de un producto es menor o igual a cero (RN-25). Se mapea a 400
/// Bad Request.
/// </summary>
public class PrecioInvalidoException(string message) : Exception(message);
