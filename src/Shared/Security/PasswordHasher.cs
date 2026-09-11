using System.Security.Cryptography;

namespace FidelixAPI.Shared.Security;

/// <summary>
/// Hashea y verifica contraseñas con PBKDF2-SHA256 (sin dependencias externas,
/// usando la implementación del BCL). Vive en `Shared` — no en
/// `BusinessLogic` — porque tanto `DataAccess` (al sembrar datos de prueba en
/// `DbInitializer`) como `BusinessLogic` (`AuthService`) necesitan hashear
/// contraseñas sin que `DataAccess` termine dependiendo de `BusinessLogic`.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    /// <summary>Genera un hash con salt aleatorio, codificado como "iteraciones.salt.hash" en Base64.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    /// <summary>Verifica una contraseña en texto plano contra un hash generado por <see cref="Hash"/>.</summary>
    public static bool Verify(string password, string hash)
    {
        var partes = hash.Split('.', 3);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[1]);
        var keyEsperada = Convert.FromBase64String(partes[2]);
        var keyIngresada = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, keyEsperada.Length);

        return CryptographicOperations.FixedTimeEquals(keyIngresada, keyEsperada);
    }
}
