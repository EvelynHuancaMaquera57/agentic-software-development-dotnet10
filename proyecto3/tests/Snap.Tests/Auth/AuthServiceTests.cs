using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Snap.Modules.Auth;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Auth;

/// <summary>Lógica de register/login con SQLite real (archivo temporal), reloj fijo y JWT verificable.</summary>
public sealed class AuthServiceTests : IAsyncLifetime, IDisposable
{
    private const string SigningKey = "unit-test-signing-key-0123456789-abcdefgh";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly TestDatabaseFile _database = new();
    private UsersRepository _users = null!;
    private AuthService _auth = null!;

    public async Task InitializeAsync()
    {
        _users = new UsersRepository(await _database.CreateInitializedAsync());
        var clock = new FixedTimeProvider(Now);
        var options = Options.Create(new JwtOptions { Issuer = "snap", Audience = "snap", SigningKey = SigningKey });
        _auth = new AuthService(_users, new PasswordHasher<UserRecord>(), new JwtTokenIssuer(options, clock), clock);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _database.Dispose();

    // ---------- register ----------

    [Fact]
    public async Task Register_ValidData_StoresUserWithHashedPasswordAndReturnsJwt()
    {
        var result = await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        Assert.True(result.Succeeded);
        var stored = await _users.FindByEmailAsync("ana@example.com");
        Assert.NotNull(stored);
        Assert.Equal("Ana", stored.Name);
        Assert.NotEqual("s3cret-pass", stored.PasswordHash);
        Assert.DoesNotContain("s3cret-pass", stored.PasswordHash, StringComparison.Ordinal);

        var jwt = await ValidateAsync(result.Token.Token);
        Assert.Equal(stored.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), jwt.Subject);
        Assert.Equal("ana@example.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercase()
    {
        await _auth.RegisterAsync("  Ana@Example.COM ", "s3cret-pass", "Ana");

        Assert.NotNull(await _users.FindByEmailAsync("ana@example.com"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.RegisterAsync("ana@example.com", "otra-pass-123", "Otra Ana");

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.EmailAlreadyRegistered, result.Error);
    }

    [Fact]
    public async Task Register_DuplicateEmailWithDifferentCase_Fails()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.RegisterAsync("ANA@EXAMPLE.COM", "otra-pass-123", "Otra Ana");

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.EmailAlreadyRegistered, result.Error);
    }

    [Theory]
    [InlineData(null, "s3cret-pass", "Ana")]
    [InlineData("no-es-un-email", "s3cret-pass", "Ana")]
    [InlineData("ana@example.com", null, "Ana")]
    [InlineData("ana@example.com", "corta", "Ana")]
    [InlineData("ana@example.com", "s3cret-pass", "   ")]
    public async Task Register_InvalidInput_FailsWithoutStoringUser(string? email, string? password, string? name)
    {
        var result = await _auth.RegisterAsync(email, password, name);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidInput, result.Error);
        Assert.Null(await _users.FindByEmailAsync("ana@example.com"));
    }

    // ---------- JWT ----------

    [Fact]
    public async Task Jwt_ExpiresIn24Hours()
    {
        var result = await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        Assert.True(result.Succeeded);
        Assert.Equal(Now.AddHours(24), result.Token.ExpiresAt);

        var jwt = new JsonWebToken(result.Token.Token);
        Assert.Equal(Now.UtcDateTime, jwt.IssuedAt);
        Assert.Equal(Now.AddHours(24).UtcDateTime, jwt.ValidTo);
    }

    // ---------- login ----------

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwt()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.LoginAsync("ana@example.com", "s3cret-pass");

        Assert.True(result.Succeeded);
        var jwt = await ValidateAsync(result.Token.Token);
        Assert.Equal("ana@example.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Ana", jwt.GetClaim(JwtRegisteredClaimNames.Name).Value);
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.LoginAsync("ana@example.com", "password-incorrecto");

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_UnknownEmail_Fails()
    {
        var result = await _auth.LoginAsync("nadie@example.com", "s3cret-pass");

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnSameMessage()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var unknown = await _auth.LoginAsync("nadie@example.com", "s3cret-pass");
        var wrong = await _auth.LoginAsync("ana@example.com", "password-incorrecto");

        Assert.Equal(unknown.Message, wrong.Message);
    }

    [Fact]
    public async Task Login_UppercaseEmail_Succeeds()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.LoginAsync("ANA@EXAMPLE.COM", "s3cret-pass");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Login_PasswordIsCaseSensitive()
    {
        await _auth.RegisterAsync("ana@example.com", "s3cret-pass", "Ana");

        var result = await _auth.LoginAsync("ana@example.com", "S3CRET-PASS");

        Assert.False(result.Succeeded);
    }

    /// <summary>Valida firma, issuer y audience. La expiración se comprueba aparte con el reloj fijo.</summary>
    private static async Task<JsonWebToken> ValidateAsync(string token)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "snap",
            ValidAudience = "snap",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = false,
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }
}
