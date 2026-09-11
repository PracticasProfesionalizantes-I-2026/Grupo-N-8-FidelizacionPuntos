namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La configuración de una regla de acumulación es inválida, ya sea por
/// valores incorrectos o por solaparse con una versión ya activa (RN-18). Se
/// mapea a 400 Bad Request.
/// </summary>
public class ReglaAcumulacionInvalidaException(string message) : Exception(message);
