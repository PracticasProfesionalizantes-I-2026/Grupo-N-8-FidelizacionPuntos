using FidelixAPI.BusinessLogic.Interfaces;
using Microsoft.Extensions.Logging;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>
/// Implementación provisoria de <see cref="IEmailSender"/> que solo loguea el
/// envío (no hay proveedor de email real todavía). Reemplazar por un
/// `SmtpEmailSender`/`SendGridEmailSender` en una fase posterior sin tocar
/// `AuthService` (OCP): solo cambia qué implementación se registra en DI.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        logger.LogInformation("Email a {Destinatario} - Asunto: {Asunto}\n{Cuerpo}", destinatario, asunto, cuerpo);
        return Task.CompletedTask;
    }
}
