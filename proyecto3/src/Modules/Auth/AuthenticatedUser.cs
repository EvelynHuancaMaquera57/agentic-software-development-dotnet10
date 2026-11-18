using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Snap.Modules.Auth;

/// <summary>
/// Usuario autenticado de la request, leído de los claims del JWT ya verificado por el middleware.
/// Se puede recibir directamente como parámetro en un endpoint protegido.
/// </summary>
public sealed record AuthenticatedUser(long Id, string Email, string Name)
{
    public static AuthenticatedUser From(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        var name = principal.FindFirstValue(JwtRegisteredClaimNames.Name);

        if (!long.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || email is null || name is null)
        {
            throw new InvalidOperationException(
                "La request no tiene un usuario autenticado válido. ¿Falta RequireAuthorization() en el endpoint?");
        }

        return new AuthenticatedUser(id, email, name);
    }

    /// <summary>Permite declarar <c>AuthenticatedUser user</c> como parámetro de un endpoint Minimal API.</summary>
    public static ValueTask<AuthenticatedUser?> BindAsync(HttpContext context) =>
        ValueTask.FromResult<AuthenticatedUser?>(From(context.User));
}
