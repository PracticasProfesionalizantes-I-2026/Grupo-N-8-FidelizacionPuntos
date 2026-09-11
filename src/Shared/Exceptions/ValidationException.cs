namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Error de validación de negocio genérico, no cubierto por una excepción más
/// específica (ej. campos obligatorios faltantes tras normalización). Se
/// mapea a 400 Bad Request en el controller.
/// </summary>
public class ValidationException(string message) : Exception(message);
