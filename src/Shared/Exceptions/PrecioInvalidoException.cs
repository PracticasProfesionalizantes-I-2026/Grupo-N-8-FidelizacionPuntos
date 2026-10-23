namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El precio de un producto debe ser mayor a 0 (RN-25). Se mapea 400 Bad Request.
/// </summary>
public class PrecioInvalidoException(string message) : Exception(message);
