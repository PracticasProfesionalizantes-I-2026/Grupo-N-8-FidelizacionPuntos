using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>
/// Implementación de desarrollo de <see cref="IEmailSender"/>: en vez de mandar el email lo
/// escribe en el log de la consola. Para producción se reemplaza por un envío SMTP real
/// registrando otra implementación en `Program.cs` (versión futura).
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <summary>Escribe el email en el log (destinatario, asunto y cuerpo).</summary>
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        logger.LogInformation("[EMAIL] Para: {Destinatario} | Asunto: {Asunto} | {Cuerpo}", destinatario, asunto, cuerpo);
        return Task.CompletedTask;
    }
}
