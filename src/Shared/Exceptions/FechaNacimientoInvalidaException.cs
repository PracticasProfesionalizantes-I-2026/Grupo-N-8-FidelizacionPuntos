namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La fecha de nacimiento del cliente es futura o no corresponde a una edad entre 18 y 120 años.
/// Se mapea a 400 Bad Request.
/// </summary>
public class FechaNacimientoInvalidaException(string message) : Exception(message);
