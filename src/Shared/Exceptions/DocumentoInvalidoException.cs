namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El documento del cliente no respeta el formato válido (solo números, de 7 a 10 dígitos). Se mapea 400 Bad Request (CU-10, flujo 3b).
/// </summary>
public class DocumentoInvalidoException(string message) : Exception(message);
