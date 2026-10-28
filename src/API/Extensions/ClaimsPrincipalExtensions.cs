using System.Security.Claims;

namespace FidelixAPI.API.Extensions;

/// <summary>Lectura de los datos del token JWT que dejó el login (CU-02, CU-09, CU-14).</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Id de la cuenta autenticada (claim NameIdentifier), o `null` si no hay un Guid válido.</summary>
    public static Guid? ObtenerId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Rol de la cuenta autenticada (claim Role), o "Anonimo" si no hay token.</summary>
    public static string ObtenerRol(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? "Anonimo";
}
