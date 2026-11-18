using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Snap.Modules.Auth;

public static class AuthModule
{
    /// <summary>Margen para relojes desincronizados entre servidores (por defecto serían 5 minutos).</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options => options.IsValid(),
                $"Configuración Jwt inválida: Issuer y Audience son obligatorios y SigningKey debe tener al menos {JwtOptions.MinSigningKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher<UserRecord>, PasswordHasher<UserRecord>>();
        services.AddSingleton<JwtTokenIssuer>();
        services.AddSingleton<UsersRepository>();
        services.AddSingleton<AuthService>();

        // Middleware de autenticación: lee "Authorization: Bearer <jwt>", lo verifica y rellena HttpContext.User.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var settings = jwt.Value;
                bearer.MapInboundClaims = false; // conserva los nombres originales: sub, email, name
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], // rechaza "alg": "none" y otros algoritmos
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = ClockSkew,
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };
            });

        services.AddAuthorization();
        return services;
    }
}
