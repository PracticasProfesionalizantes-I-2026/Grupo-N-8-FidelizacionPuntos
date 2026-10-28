namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// La configuración de la regla de acumulación es inconsistente: valores no positivos
/// o una fecha de inicio que se superpone con otra versión (RN-18). Se mapea 400 Bad Request.
/// </summary>
public class ReglaAcumulacionInvalidaException(string message) : Exception(message);
