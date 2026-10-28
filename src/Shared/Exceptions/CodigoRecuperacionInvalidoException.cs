namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El código de recuperación no coincide, venció o ya se usó (RN-27). Se mapea 400 Bad Request (CU-25, flujo 5a).
/// </summary>
public class CodigoRecuperacionInvalidoException(string message) : Exception(message);
