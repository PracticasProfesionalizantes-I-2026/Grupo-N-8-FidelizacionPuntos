namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El código de recuperación de contraseña no existe, ya venció o ya fue
/// utilizado (RN-27). Se mapea a 400 Bad Request.
/// </summary>
public class CodigoRecuperacionInvalidoException(string message) : Exception(message);
