using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FidelixAPI.BusinessLogic.Interfaces;

namespace FidelixAPI.API.IntegrationTests.Fakes;

/// <summary>
/// Email falso para los tests: en vez de enviar, guarda los emails en memoria. Así el test de
/// recuperación de contraseña (CU-25) puede leer el código como lo haría el usuario en su casilla.
/// </summary>
public class EmailSenderEnMemoria : IEmailSender
{
    /// <summary> Emails "enviados": (destinatario, asunto, cuerpo). </summary>
    public ConcurrentQueue<(string Destinatario, string Asunto, string Cuerpo)> Enviados { get; } = new();

    /// <summary> Guarda el email en vez de enviarlo. </summary>
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        Enviados.Enqueue((destinatario, asunto, cuerpo));
        return Task.CompletedTask;
    }

    /// <summary> Último código de 6 dígitos enviado a ese destinatario, o `null` si no recibió ninguno. </summary>
    public string? UltimoCodigoPara(string destinatario) =>
        Enviados.Where(e => e.Destinatario == destinatario)
            .Select(e => Regex.Match(e.Cuerpo, @"\b\d{6}\b").Value)
            .LastOrDefault(c => c.Length == 6);
}
