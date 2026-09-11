namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El costo en puntos de un beneficio es menor o igual a cero (RN-16). Se
/// mapea a 400 Bad Request.
/// </summary>
public class CostoInvalidoException(string message) : Exception(message);
