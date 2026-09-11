namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Ya existe un producto con el mismo nombre (RN-24). Se mapea a 409
/// Conflict.
/// </summary>
public class ProductoDuplicadoException(string message) : Exception(message);
