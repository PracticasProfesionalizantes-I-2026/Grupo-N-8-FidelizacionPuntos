namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No existe el beneficio solicitado. Se mapea a 404 Not Found.
/// </summary>
public class BeneficioNotFoundException(string message) : Exception(message);
