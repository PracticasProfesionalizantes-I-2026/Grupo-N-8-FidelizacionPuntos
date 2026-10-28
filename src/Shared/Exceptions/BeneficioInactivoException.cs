namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El beneficio existe pero está desactivado y no se puede canjear (RN-17). Se mapea 409 Conflict (CU-07, flujo 3b).
/// </summary>
public class BeneficioInactivoException(string message) : Exception(message);
