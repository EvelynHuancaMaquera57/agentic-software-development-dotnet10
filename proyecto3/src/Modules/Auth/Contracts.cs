using System.Diagnostics.CodeAnalysis;

namespace Snap.Modules.Auth;

/// <summary>Fila de la tabla <c>users</c>.</summary>
public sealed record UserRecord(long Id, string Email, string PasswordHash, string Name, DateTimeOffset CreatedAt);

public sealed record AuthToken(string Token, DateTimeOffset ExpiresAt);

public sealed record RegisterRequest(string? Email, string? Password, string? Name);

public sealed record LoginRequest(string? Email, string? Password);

/// <summary>Datos públicos del usuario. Nunca incluye el hash del password.</summary>
public sealed record UserResponse(long Id, string Email, string Name, DateTimeOffset CreatedAt)
{
    public static UserResponse From(UserRecord user) => new(user.Id, user.Email, user.Name, user.CreatedAt);
}

public sealed record AuthResponse(UserResponse User, string Token, DateTimeOffset ExpiresAt)
{
    public static AuthResponse From(UserRecord user, AuthToken token) =>
        new(UserResponse.From(user), token.Token, token.ExpiresAt);
}

public enum AuthError
{
    InvalidInput,
    EmailAlreadyRegistered,
    InvalidCredentials,
}

/// <summary>Resultado de register/login: un token o un error esperado (no se usan excepciones para el flujo normal).</summary>
public sealed record AuthResult
{
    public UserRecord? User { get; private init; }
    public AuthToken? Token { get; private init; }
    public AuthError? Error { get; private init; }
    public string? Message { get; private init; }

    /// <summary>Campo de entrada que provocó un <see cref="AuthError.InvalidInput"/> (email, password o name).</summary>
    public string? Field { get; private init; }

    [MemberNotNullWhen(true, nameof(User), nameof(Token))]
    [MemberNotNullWhen(false, nameof(Error), nameof(Message))]
    public bool Succeeded => Token is not null;

    public static AuthResult Success(UserRecord user, AuthToken token) => new() { User = user, Token = token };

    public static AuthResult Failure(AuthError error, string message, string? field = null) =>
        new() { Error = error, Message = message, Field = field };
}
