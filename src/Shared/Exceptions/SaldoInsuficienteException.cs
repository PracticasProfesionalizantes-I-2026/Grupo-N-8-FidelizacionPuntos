namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El cliente no tiene saldo de puntos suficiente para completar el canje
/// (RN-08). Se mapea a 409 Conflict.
/// </summary>
public class SaldoInsuficienteException(string message) : Exception(message);
