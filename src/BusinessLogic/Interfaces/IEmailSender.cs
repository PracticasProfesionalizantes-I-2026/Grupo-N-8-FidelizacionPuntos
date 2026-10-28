namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Envío de emails (códigos de recuperación CU-25, aviso de puntos vencidos CU-22). Es una
/// interfaz para poder cambiar la implementación (SMTP, SendGrid...) sin tocar los services.
/// </summary>
public interface IEmailSender
{
    /// <summary>Envía un email al destinatario indicado.</summary>
    Task EnviarAsync(string destinatario, string asunto, string cuerpo);
}
