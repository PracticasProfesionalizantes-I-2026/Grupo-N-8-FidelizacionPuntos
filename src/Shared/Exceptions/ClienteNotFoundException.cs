namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// No existe el cliente solicitado (por Id o por documento). Se mapea a 404
/// Not Found.
/// </summary>
public class ClienteNotFoundException(string message) : Exception(message);
