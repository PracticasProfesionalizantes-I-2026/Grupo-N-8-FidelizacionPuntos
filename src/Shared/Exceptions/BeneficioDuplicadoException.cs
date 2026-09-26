namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Ya existe un beneficio con el mismo nombre (RN-23). Se mapea a 409
/// Conflict.
/// </summary>
public class BeneficioDuplicadoException(string message) : Exception(message);
