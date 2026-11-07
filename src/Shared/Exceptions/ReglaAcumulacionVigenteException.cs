namespace FidelixAPI.Shared.Exceptions;

/// <summary>
/// Se intentó modificar una regla de acumulación que ya entró en vigencia (CU-19). No se permite,
/// porque cambiaría con qué regla se calcularon compras ya registradas: para cambiar el criterio se
/// crea una versión nueva con fecha futura. Se mapea a 409 Conflict.
/// </summary>
public class ReglaAcumulacionVigenteException(string message) : Exception(message);
