namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// El documento de identidad no se puede modificar (RN-04). Se mapea 400 Bad Request (CU-03, flujo 2b).
/// </summary>
public class DocumentoNoModificableException(string message) : Exception(message);
