namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El nuevo email indicado en una modificación de perfil ya pertenece a otro
/// cliente (RN-01, CU-03). Se mapea a 409 Conflict.
/// </summary>
public class EmailDuplicadoException(string message) : Exception(message);
