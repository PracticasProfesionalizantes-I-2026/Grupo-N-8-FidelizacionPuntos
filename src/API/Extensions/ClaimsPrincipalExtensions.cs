using System.Security.Claims;

namespace FidelixAPI.API.Extensions;

/// <summary>
/// Helper de lectura de claims del JWT. Vive en `API` (no en `BusinessLogic`)
/// porque `ClaimsPrincipal` es un tipo del framework de presentación: los
/// controllers son quienes resuelven "quién soy" y se lo pasan a los
/// services como parámetros simples (Guid), sin que BusinessLogic conozca
/// nada de HTTP/JWT.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Id (Guid) de la cuenta autenticada, emitido en el login por `AuthService`.</summary>
    public static Guid ObtenerId(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("El token no contiene el identificador del usuario.");
        return Guid.Parse(valor);
    }
}
