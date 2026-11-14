using System.Text;
using Microsoft.Extensions.Configuration;

namespace FidelixAPI.BusinessLogic.Helpers;

/// <summary>
/// Punto único de lectura de la clave de firma de los tokens (`Jwt:Key`). La clave NO se versiona
/// en `appsettings.json` (si el repositorio es visible, cualquiera podría firmar un token de Admin):
/// en desarrollo se carga con User Secrets y en producción con la variable de entorno `Jwt__Key`.
/// La usan tanto el arranque de la API (validación del token) como <c>JwtTokenService</c> (emisión),
/// así las dos puntas aplican exactamente la misma regla.
/// </summary>
public static class ClaveJwt
{
    /// <summary> HMAC-SHA256 necesita una clave de al menos 256 bits (32 bytes). </summary>
    public const int BytesMinimos = 32;

    /// <summary> Mensaje que explica cómo configurar la clave, para que el error sea accionable. </summary>
    private const string ComoConfigurar =
        "Configurala con User Secrets (desde src/: dotnet user-secrets set \"Jwt:Key\" \"<clave de 32+ caracteres>\" --project API) " +
        "o con la variable de entorno Jwt__Key.";

    /// <summary>
    /// Devuelve la clave configurada; si falta o es más corta que <see cref="BytesMinimos"/> lanza una
    /// excepción con las instrucciones, para que la API no arranque con una clave débil o inexistente.
    /// </summary>
    public static string Obtener(IConfiguration configuration)
    {
        var clave = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException($"Falta la clave de firma del token (Jwt:Key). {ComoConfigurar}");
        }

        if (Encoding.UTF8.GetByteCount(clave) < BytesMinimos)
        {
            throw new InvalidOperationException(
                $"La clave de firma del token (Jwt:Key) debe tener al menos {BytesMinimos} bytes. {ComoConfigurar}");
        }

        return clave;
    }
}
