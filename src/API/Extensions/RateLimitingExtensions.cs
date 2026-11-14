using System.Threading.RateLimiting;
using FidelixAPI.Shared.DTOs.Comun;
using Microsoft.AspNetCore.RateLimiting;

namespace FidelixAPI.API.Extensions;

/// <summary>
/// Configuración del rate limiting de la API (CRIT-04). Se separa de Program.cs para que el
/// arranque siga leyéndose como una lista de pasos y la política quede documentada en un solo lugar.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary> Nombre de la política que protege los endpoints de recuperación de contraseña. </summary>
    public const string PoliticaRecuperacion = "recuperacion";

    /// <summary>
    /// Registra la política <see cref="PoliticaRecuperacion"/>: ventana fija por IP de origen, con el
    /// máximo de pedidos y la duración leídos de `RateLimiting:Recuperacion` (por defecto 5 por minuto).
    /// Al superarla responde 429 Too Many Requests con el formato de error de la API.
    /// </summary>
    public static IServiceCollection AgregarRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitidos = configuration.GetValue("RateLimiting:Recuperacion:PedidosPorVentana", 5);
        var ventana = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Recuperacion:VentanaSegundos", 60));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (contexto, cancelacion) =>
                await contexto.HttpContext.Response.WriteAsJsonAsync(
                    new ErrorResponseDTO("Demasiados intentos. Espere unos minutos y vuelva a intentar."), cancelacion);

            // Cada IP tiene su propio contador: un atacante no puede probar códigos sin límite,
            // y un usuario legítimo no se ve afectado por los intentos de otro.
            options.AddPolicy(PoliticaRecuperacion, contextoHttp =>
                RateLimitPartition.GetFixedWindowLimiter(
                    contextoHttp.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitidos,
                        Window = ventana,
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
