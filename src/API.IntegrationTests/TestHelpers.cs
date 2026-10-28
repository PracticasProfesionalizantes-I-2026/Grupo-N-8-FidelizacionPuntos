using System.Net.Http.Headers;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Cliente;

namespace FidelixAPI.API.IntegrationTests;

/// <summary> Operaciones que repiten varios tests de integración (alta de clientes y login). </summary>
public static class TestHelpers
{
    /// <summary> Documento único de 9 dígitos, para que los tests que comparten la base no choquen (RN-01). </summary>
    public static string DocumentoUnico() => Random.Shared.Next(100_000_000, 999_999_999).ToString();

    /// <summary>
    /// Registra un cliente nuevo (CU-01) con documento y email únicos, inicia sesión (CU-02) y deja el
    /// token en el cliente HTTP. Devuelve el Id, el documento y el email del cliente creado.
    /// </summary>
    public static async Task<(Guid Id, string Documento, string Email)> RegistrarYAutenticarClienteAsync(HttpClient client, DateTime? fechaNacimiento = null)
    {
        var documento = DocumentoUnico();
        var email = $"cliente.{documento}@fidelix.local";
        var registro = await client.PostAsJsonAsync("/api/clientes/registro", new ClienteCreateDTO
        {
            Nombre = "Cliente Test",
            Documento = documento,
            Email = email,
            Password = "Cliente123!",
            FechaNacimiento = fechaNacimiento
        });
        var cliente = await registro.Content.ReadFromJsonAsync<ClienteResponseDTO>();
        await LoginClienteAsync(client, email, "Cliente123!");
        return (cliente!.Id, documento, email);
    }

    /// <summary> Inicia sesión como cliente (CU-02) y deja el token en el header del cliente HTTP. </summary>
    public static async Task LoginClienteAsync(HttpClient client, string email, string password)
    {
        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = password });
        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponseDTO>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }
}
