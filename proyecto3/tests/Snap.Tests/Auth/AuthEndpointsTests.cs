using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Snap.Modules.Auth;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Auth;

/// <summary>Pruebas de integración: peticiones HTTP reales → AuthService → SQLite. Cada test tiene su propia base.</summary>
public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private const string Password = "s3cret-pass";

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

    // ---------- POST /auth/register ----------

    [Fact]
    public async Task Register_ValidData_Returns201WithUserAndToken()
    {
        using var response = await RegisterAsync("Ana@Example.com", Password, "Ana");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.True(body.User.Id > 0);
        Assert.Equal("ana@example.com", body.User.Email);
        Assert.Equal("Ana", body.User.Name);

        var token = await ValidateTokenAsync(body.Token);
        Assert.Equal(body.User.Id.ToString(CultureInfo.InvariantCulture), token.Subject);
        Assert.Equal(body.ExpiresAt.UtcDateTime, token.ValidTo, TimeSpan.FromSeconds(1));
        Assert.Equal(TimeSpan.FromHours(24), token.ValidTo - token.IssuedAt);
    }

    [Fact]
    public async Task Register_ResponseNeverExposesPassword()
    {
        using var response = await RegisterAsync("ana@example.com", Password, "Ana");

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Password, json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("no-es-un-email")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Register_InvalidEmail_Returns400WithEmailError(string? email)
    {
        using var response = await RegisterAsync(email, Password, "Ana");

        await AssertValidationErrorAsync(response, "email");
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400WithPasswordError()
    {
        using var response = await RegisterAsync("ana@example.com", "1234567", "Ana");

        await AssertValidationErrorAsync(response, "password");
    }

    [Fact]
    public async Task Register_MissingName_Returns400WithNameError()
    {
        using var response = await RegisterAsync("ana@example.com", Password, "   ");

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        (await RegisterAsync("ana@example.com", Password, "Ana")).Dispose();

        using var response = await RegisterAsync("ANA@example.com", "otra-pass-123", "Otra Ana");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_MalformedJson_Returns400()
    {
        using var content = new StringContent("{ esto no es json", Encoding.UTF8, "application/json");

        using var response = await _client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /auth/login ----------

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithUserAndToken()
    {
        var registered = await RegisterUserAsync("ana@example.com", "Ana");

        using var response = await LoginAsync("ana@example.com", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal(registered.User, body.User);

        var token = await ValidateTokenAsync(body.Token);
        Assert.Equal(registered.User.Id.ToString(CultureInfo.InvariantCulture), token.Subject);
    }

    [Fact]
    public async Task Login_UppercaseEmail_Returns200()
    {
        await RegisterUserAsync("ana@example.com", "Ana");

        using var response = await LoginAsync("  ANA@EXAMPLE.COM ", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await RegisterUserAsync("ana@example.com", "Ana");

        using var response = await LoginAsync("ana@example.com", "password-incorrecto");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var response = await LoginAsync("nadie@example.com", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_AreIndistinguishable()
    {
        await RegisterUserAsync("ana@example.com", "Ana");

        using var unknown = await LoginAsync("nadie@example.com", Password);
        using var wrong = await LoginAsync("ana@example.com", "password-incorrecto");

        Assert.Equal(unknown.StatusCode, wrong.StatusCode);
        Assert.Equal(await DetailAsync(unknown), await DetailAsync(wrong));
    }

    [Theory]
    [InlineData(null, Password, "email")]
    [InlineData("ana@example.com", null, "password")]
    [InlineData("ana@example.com", "", "password")]
    public async Task Login_MissingField_Returns400WithFieldError(string? email, string? password, string field)
    {
        using var response = await LoginAsync(email, password);

        await AssertValidationErrorAsync(response, field);
    }

    // ---------- helpers ----------

    private Task<HttpResponseMessage> RegisterAsync(string? email, string? password, string? name) =>
        _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, password, name));

    private Task<HttpResponseMessage> LoginAsync(string? email, string? password) =>
        _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));

    private async Task<AuthResponse> RegisterUserAsync(string email, string name)
    {
        using var response = await RegisterAsync(email, Password, name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = json.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out var messages), $"Se esperaba un error para '{field}': {errors}");
        Assert.NotEmpty(messages.EnumerateArray());
    }

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("detail").GetString();
    }

    /// <summary>Comprueba firma, issuer, audience y vigencia con la clave que usa la app en los tests.</summary>
    private static async Task<JsonWebToken> ValidateTokenAsync(string token)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "snap",
            ValidAudience = "snap",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SnapApiFactory.TestSigningKey)),
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }
}
