namespace FidelixAPI.Shared.Configuration;

/// <summary>Configuración del JWT emitido en el login, leída de `appsettings.json` (sección "Jwt").</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
