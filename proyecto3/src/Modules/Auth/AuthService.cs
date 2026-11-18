using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace Snap.Modules.Auth;

/// <summary>Registro y login. No sabe nada de HTTP.</summary>
public sealed class AuthService
{
    public const int MinPasswordLength = 8;
    public const int MaxNameLength = 100;
    private const int MaxEmailLength = 254;

    private readonly UsersRepository _users;
    private readonly IPasswordHasher<UserRecord> _hasher;
    private readonly JwtTokenIssuer _tokens;
    private readonly TimeProvider _clock;

    // Hash de relleno: si el email no existe se verifica contra él igualmente, para que
    // "usuario inexistente" y "password incorrecto" tarden lo mismo (evita enumerar emails).
    private readonly string _dummyHash;

    public AuthService(UsersRepository users, IPasswordHasher<UserRecord> hasher, JwtTokenIssuer tokens, TimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
        _dummyHash = hasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
    }

    private static readonly UserRecord DummyUser = new(0, string.Empty, string.Empty, string.Empty, DateTimeOffset.MinValue);

    /// <summary>Emails sin espacios alrededor y en minúsculas, para que Ana@X.com y ana@x.com sean la misma cuenta.</summary>
#pragma warning disable CA1308 // Requisito explícito: los emails se guardan en minúsculas.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
#pragma warning restore CA1308

    public async Task<AuthResult> RegisterAsync(
        string? email, string? password, string? name, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email is null ? null : NormalizeEmail(email);
        var trimmedName = name?.Trim();

        if (!IsValidEmail(normalizedEmail))
        {
            return AuthResult.Failure(AuthError.InvalidInput, "El email no es válido.", "email");
        }
        if (password is null || password.Length < MinPasswordLength)
        {
            return AuthResult.Failure(AuthError.InvalidInput, $"El password debe tener al menos {MinPasswordLength} caracteres.", "password");
        }
        if (string.IsNullOrEmpty(trimmedName) || trimmedName.Length > MaxNameLength)
        {
            return AuthResult.Failure(AuthError.InvalidInput, $"El nombre es obligatorio y no puede superar {MaxNameLength} caracteres.", "name");
        }

        var createdAt = _clock.GetUtcNow();
        var passwordHash = _hasher.HashPassword(DummyUser, password);

        var id = await _users.TryInsertAsync(normalizedEmail, passwordHash, trimmedName, createdAt, cancellationToken);
        if (id is null)
        {
            return AuthResult.Failure(AuthError.EmailAlreadyRegistered, "Ya existe una cuenta con ese email.");
        }

        var user = new UserRecord(id.Value, normalizedEmail, passwordHash, trimmedName, createdAt);
        return AuthResult.Success(user, _tokens.Issue(user));
    }

    public async Task<AuthResult> LoginAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        // Campos ausentes = petición mal formada (400). Valores incorrectos = credenciales inválidas (401).
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthResult.Failure(AuthError.InvalidInput, "El email es obligatorio.", "email");
        }
        if (string.IsNullOrEmpty(password))
        {
            return AuthResult.Failure(AuthError.InvalidInput, "El password es obligatorio.", "password");
        }

        var invalid = AuthResult.Failure(AuthError.InvalidCredentials, "Email o password incorrectos.");

        var user = await _users.FindByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null)
        {
            _hasher.VerifyHashedPassword(DummyUser, _dummyHash, password);
            return invalid;
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return invalid;
        }

        // Si el hash se creó con parámetros antiguos (menos iteraciones), se actualiza de forma transparente.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await _users.UpdatePasswordHashAsync(user.Id, _hasher.HashPassword(user, password), cancellationToken);
        }

        return AuthResult.Success(user, _tokens.Issue(user));
    }

    private static bool IsValidEmail([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? email) =>
        email is { Length: > 0 and <= MaxEmailLength }
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email;
}
