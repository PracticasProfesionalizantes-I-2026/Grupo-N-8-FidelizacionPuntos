using System.Net.Http.Headers;
using System.Net.Http.Json;
using FidelixAPI.Shared.DTOs.Auth;

namespace FidelixAPI.API.IntegrationTests;

/// <summary>Loguea contra el `AuthController` real y devuelve un `HttpClient` con el JWT ya seteado.</summary>
public static class TestAuthHelper
{
    public static async Task<HttpClient> ClienteAutenticadoAsync(this FidelixApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var respuesta = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = email, Password = password });
        var body = await respuesta.Content.ReadFromJsonAsync<LoginResponseDTO>()
            ?? throw new InvalidOperationException("El login de prueba falló: no se obtuvo un token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return client;
    }
}
