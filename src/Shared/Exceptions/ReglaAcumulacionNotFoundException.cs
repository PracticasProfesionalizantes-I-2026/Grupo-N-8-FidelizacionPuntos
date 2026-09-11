namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No existe la regla de acumulación solicitada por Id. Se mapea a 404 Not
/// Found. (Excepción nueva, introducida al cerrar el gap de lectura de CU-19.)
/// </summary>
public class ReglaAcumulacionNotFoundException(string message) : Exception(message);
