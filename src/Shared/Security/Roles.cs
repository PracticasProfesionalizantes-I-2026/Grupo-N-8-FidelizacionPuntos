namespace FidelixAPI.Shared.Security;

/// <summary>
/// Nombres de los roles que viajan en el token JWT (claim Role). Se usan en
/// `[Authorize(Roles = ...)]` para que cada actor acceda solo a lo suyo (RN-11).
/// </summary>
public static class Roles
{
    public const string Cliente = "Cliente";
    public const string Empleado = "Empleado";
    public const string Admin = "Admin";
}
