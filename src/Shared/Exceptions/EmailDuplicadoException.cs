namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El email ya pertenece a otra cuenta (RN-01). Se mapea 409 Conflict (CU-03, flujo 3a).
/// </summary>
public class EmailDuplicadoException(string message) : Exception(message);
