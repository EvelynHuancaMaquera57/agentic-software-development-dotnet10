using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Snap.Modules.Auth;

/// <summary>
/// Capa HTTP de autenticación. Traduce <see cref="AuthResult"/> a códigos de estado:
/// 400 entrada inválida (con el campo), 401 credenciales incorrectas, 409 email ya registrado.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth");
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);

        // Protegido: sin token válido el middleware responde 401 antes de llegar aquí.
        auth.MapGet("/me", (AuthenticatedUser user) => TypedResults.Ok(user)).RequireAuthorization();
        return app;
    }

    private static async Task<Results<Created<AuthResponse>, ValidationProblem, ProblemHttpResult>> RegisterAsync(
        RegisterRequest request, AuthService auth, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterAsync(request.Email, request.Password, request.Name, cancellationToken);
        if (result.Succeeded)
        {
            // No hay recurso GET /users/{id} todavía, así que el 201 va sin cabecera Location.
            return TypedResults.Created((string?)null, AuthResponse.From(result.User, result.Token));
        }

        return result.Error == AuthError.InvalidInput ? InvalidInput(result) : Problem(result);
    }

    private static async Task<Results<Ok<AuthResponse>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        LoginRequest request, AuthService auth, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, cancellationToken);
        if (result.Succeeded)
        {
            return TypedResults.Ok(AuthResponse.From(result.User, result.Token));
        }

        return result.Error == AuthError.InvalidInput ? InvalidInput(result) : Problem(result);
    }

    private static ValidationProblem InvalidInput(AuthResult result) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [result.Field ?? "request"] = [result.Message!],
        });

    private static ProblemHttpResult Problem(AuthResult result) => TypedResults.Problem(
        statusCode: result.Error switch
        {
            AuthError.EmailAlreadyRegistered => StatusCodes.Status409Conflict,
            AuthError.InvalidCredentials => StatusCodes.Status401Unauthorized,
            _ => throw new UnreachableException($"Error de auth sin código HTTP asignado: {result.Error}"),
        },
        detail: result.Message);
}
