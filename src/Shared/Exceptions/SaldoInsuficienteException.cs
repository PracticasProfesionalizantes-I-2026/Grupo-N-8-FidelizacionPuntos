namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El cliente no tiene puntos disponibles suficientes para el beneficio (RN-08). Se mapea 409 Conflict (CU-07, flujo 3c).
/// </summary>
public class SaldoInsuficienteException(string message) : Exception(message);
