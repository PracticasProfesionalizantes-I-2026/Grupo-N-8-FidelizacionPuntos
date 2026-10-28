namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Un ítem de la compra referencia un producto inexistente o inactivo (RN-26). Se mapea 400 Bad Request (CU-10, flujo 2a).
/// </summary>
public class ItemCompraInvalidoException(string message) : Exception(message);
