using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Snap.Modules.Auth;

/// <summary>Sección "Jwt" de la configuración. En producción, SigningKey llega por variable de entorno (Jwt__SigningKey).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSigningKeyBytes = 32; // HMAC-SHA256 necesita al menos 256 bits

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>La misma clave firma (JwtTokenIssuer) y verifica (middleware JwtBearer).</summary>
    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && Encoding.UTF8.GetByteCount(SigningKey) >= MinSigningKeyBytes;
}
