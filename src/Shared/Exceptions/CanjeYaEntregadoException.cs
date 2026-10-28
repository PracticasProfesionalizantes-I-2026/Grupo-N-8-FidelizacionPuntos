namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El canje ya se había entregado: no se puede entregar dos veces (RN-10). Se mapea 409 Conflict.
/// </summary>
public class CanjeYaEntregadoException(string message) : Exception(message);
