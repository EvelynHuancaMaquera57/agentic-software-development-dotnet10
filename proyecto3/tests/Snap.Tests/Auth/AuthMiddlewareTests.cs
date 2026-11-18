using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Snap.Modules.Auth;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Auth;

/// <summary>
/// Middleware JWT probado con peticiones HTTP reales contra GET /auth/me (endpoint protegido).
/// Los tokens inválidos se fabrican a mano para cubrir cada forma de fallo.
/// </summary>
public sealed class AuthMiddlewareTests : IAsyncLifetime
{
    private const string ProtectedPath = "/auth/me";

    private readonly SnapApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- token válido ----------

    [Fact]
    public async Task ValidToken_PassesAndAttachesUserToRequest()
    {
        var registered = await RegisterAsync("ana@example.com", "Ana");

        using var response = await GetProtectedAsync("Bearer", registered.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
        Assert.Equal(new AuthenticatedUser(registered.User.Id, "ana@example.com", "Ana"), user);
    }

    [Fact]
    public async Task ValidToken_FromLogin_Passes()
    {
        await RegisterAsync("ana@example.com", "Ana");
        using var login = await _client.PostAsJsonAsync("/auth/login", new LoginRequest("ana@example.com", "s3cret-pass"));
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Token;

        using var response = await GetProtectedAsync("Bearer", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_SchemeIsCaseInsensitive()
    {
        var registered = await RegisterAsync("ana@example.com", "Ana");

        using var response = await GetProtectedAsync("bearer", registered.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- sin token ----------

    [Fact]
    public async Task NoAuthorizationHeader_Returns401WithBearerChallenge()
    {
        using var response = await _client.GetAsync(ProtectedPath);

        AssertUnauthorized(response);
    }

    [Fact]
    public async Task EmptyBearerToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ProtectedPath);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer ");

        using var response = await _client.SendAsync(request);

        AssertUnauthorized(response);
    }

    [Fact]
    public async Task WrongScheme_Returns401()
    {
        var registered = await RegisterAsync("ana@example.com", "Ana");

        using var response = await GetProtectedAsync("Basic", registered.Token);

        AssertUnauthorized(response);
    }

    // ---------- token expirado o malformado ----------

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        var now = DateTime.UtcNow;
        var expired = CreateToken(issuedAt: now.AddHours(-25), expires: now.AddHours(-1));

        using var response = await GetProtectedAsync("Bearer", expired);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task TokenExpiredJustBeyondClockSkew_Returns401()
    {
        var now = DateTime.UtcNow;
        var expired = CreateToken(issuedAt: now.AddHours(-24), expires: now - AuthModule.ClockSkew - TimeSpan.FromSeconds(5));

        using var response = await GetProtectedAsync("Bearer", expired);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Theory]
    [InlineData("esto-no-es-un-jwt")]
    [InlineData("aaa.bbb.ccc")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.e30")]
    public async Task MalformedToken_Returns401(string token)
    {
        using var response = await GetProtectedAsync("Bearer", token);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task TamperedPayload_Returns401()
    {
        var registered = await RegisterAsync("ana@example.com", "Ana");
        var parts = registered.Token.Split('.');
        var forgedPayload = Base64UrlEncoder.Encode("""{"sub":"999","email":"admin@example.com","name":"Admin","iss":"snap","aud":"snap","exp":4102444800}""");

        using var response = await GetProtectedAsync("Bearer", $"{parts[0]}.{forgedPayload}.{parts[2]}");

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKey_Returns401()
    {
        var token = CreateToken(signingKey: "otra-clave-de-un-atacante-0123456789-abcdef");

        using var response = await GetProtectedAsync("Bearer", token);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task UnsignedToken_AlgNone_Returns401()
    {
        var token = CreateToken(signingKey: null);

        using var response = await GetProtectedAsync("Bearer", token);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task TokenFromAnotherIssuer_Returns401()
    {
        var token = CreateToken(issuer: "otro-servicio");

        using var response = await GetProtectedAsync("Bearer", token);

        AssertUnauthorized(response, expectInvalidToken: true);
    }

    [Fact]
    public async Task HandCraftedValidToken_Passes()
    {
        // Control: el mismo generador de tokens de estos tests produce un token aceptado cuando todo es correcto.
        using var response = await GetProtectedAsync("Bearer", CreateToken());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- rutas públicas ----------

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/urls")]
    public async Task PublicRoutes_DoNotRequireToken(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- helpers ----------

    private async Task<AuthResponse> RegisterAsync(string email, string name)
    {
        using var response = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, "s3cret-pass", name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private async Task<HttpResponseMessage> GetProtectedAsync(string scheme, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ProtectedPath);
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);
        return await _client.SendAsync(request);
    }

    private static void AssertUnauthorized(HttpResponseMessage response, bool expectInvalidToken = false)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        if (expectInvalidToken)
        {
            Assert.Contains("error=\"invalid_token\"", challenge.Parameter, StringComparison.Ordinal);
        }
    }

    /// <summary>Fabrica un JWT con los mismos claims que emite la app, permitiendo romper una pieza cada vez.</summary>
    private static string CreateToken(
        DateTime? issuedAt = null,
        DateTime? expires = null,
        string? signingKey = SnapApiFactory.TestSigningKey,
        string issuer = "snap")
    {
        var iat = issuedAt ?? DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = "snap",
            IssuedAt = iat,
            NotBefore = iat,
            Expires = expires ?? iat.AddHours(24),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = "1",
                [JwtRegisteredClaimNames.Email] = "ana@example.com",
                [JwtRegisteredClaimNames.Name] = "Ana",
            },
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
