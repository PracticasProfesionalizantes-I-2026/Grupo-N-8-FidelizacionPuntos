namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Envío de emails transaccionales (CU-25: código de recuperación). Es una
/// abstracción a propósito (DIP): qué proveedor de correo se use (SMTP,
/// SendGrid, etc.) es un detalle de infraestructura que no debe filtrarse a
/// `AuthService` ni obligar a `BusinessLogic` a referenciar un SDK externo.
/// </summary>
public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo);
}
