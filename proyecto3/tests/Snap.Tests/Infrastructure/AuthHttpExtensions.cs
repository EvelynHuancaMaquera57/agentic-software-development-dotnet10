using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Snap.Modules.Auth;

namespace Snap.Tests.Infrastructure;

/// <summary>Atajos para tests de integración que necesitan un usuario autenticado.</summary>
public static class AuthHttpExtensions
{
    public const string DefaultPassword = "s3cret-pass";

    /// <summary>Registra un usuario vía POST /auth/register y devuelve su usuario y token.</summary>
    public static async Task<AuthResponse> RegisterUserAsync(this HttpClient client, string email, string name = "Test User")
    {
        using var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, DefaultPassword, name));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"No se pudo registrar {email}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    /// <summary>Envía la petición con "Authorization: Bearer token" sin tocar los headers por defecto del cliente.</summary>
    public static Task<HttpResponseMessage> SendAuthenticatedAsync(
        this HttpClient client, HttpMethod method, string path, string token, object? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (jsonBody is not null)
        {
            request.Content = JsonContent.Create(jsonBody);
        }
        return client.SendAsync(request);
    }
}
