namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Se intentó canjear un beneficio que fue desactivado por el admin (RN-17).
/// Se mapea a 409 Conflict.
/// </summary>
public class BeneficioInactivoException(string message) : Exception(message);
