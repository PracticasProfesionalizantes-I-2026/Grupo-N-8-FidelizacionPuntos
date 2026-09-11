namespace FidelixAPI.Shared.Enums;

/// <summary>
/// Representa el rol de un usuario autenticado en el sistema (Cliente, Empleado o Admin).
/// Se usa para el claim de rol del JWT y para enrutar el código de recuperación
/// de contraseña (CU-25) según quién lo solicita.
/// </summary>
public enum RolUsuario
{
    Cliente,
    Empleado,
    Admin
}
