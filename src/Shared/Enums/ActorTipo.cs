namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Identifica quién originó una operación auditada (CU-21): un usuario humano
/// con sesión (Empleado/Admin) o un proceso automático sin actor humano
/// (Sistema, ej. CU-22 vencimiento de puntos o CU-26 bono de cumpleaños).
/// </summary>
public enum ActorTipo
{
    Empleado,
    Admin,
    Sistema
}
