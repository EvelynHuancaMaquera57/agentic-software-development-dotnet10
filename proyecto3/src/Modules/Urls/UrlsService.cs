using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Snap.Modules.Urls;

/// <summary>Reglas de negocio del acortador. No sabe nada de HTTP.</summary>
public sealed partial class UrlsService(UrlsRepository repository, IShortCodeGenerator codes, TimeProvider clock)
{
    public const int MaxCodeAttempts = 5;
    public const int MaxUrlLength = 2048;
    public const string InvalidUrlMessage =
        "La URL no es válida. Usa una dirección web como www.ejemplo.com o https://www.ejemplo.com/ruta.";

    public const int MinAliasLength = 3;
    public const int MaxAliasLength = 30;
    public const string InvalidAliasMessage =
        "El alias solo puede tener letras, números, guiones (-) y guiones bajos (_), entre 3 y 30 caracteres.";
    public const string ReservedAliasMessage = "Ese alias está reservado. Elige otro.";
    public const string AliasTakenMessage = "Ese alias ya está en uso. Elige otro.";

    /// <summary>
    /// Alias que chocarían con rutas de la API o de la interfaz web (sin distinguir mayúsculas).
    /// Las rutas literales ganan a /{code}, así que un alias "health" nunca redirigiría.
    /// </summary>
    public static readonly IReadOnlySet<string> ReservedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "api", "app", "auth", "health", "login", "logout", "register", "dashboard", "admin", "static", "assets",
    };

    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const string InvalidPageMessage = "La página debe ser 1 o mayor.";
    public const string InvalidPageSizeMessage = "El tamaño de página debe estar entre 1 y 100.";

    /// <summary>Mensaje de error del alias, o null si es válido. Se espera ya recortado (Trim).</summary>
    public static string? GetAliasError(string alias)
    {
        if (!AliasRegex().IsMatch(alias))
        {
            return InvalidAliasMessage;
        }
        return ReservedAliases.Contains(alias) ? ReservedAliasMessage : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{3,30}$")]
    private static partial Regex AliasRegex();

    /// <summary>
    /// Convierte lo que escribe el usuario en una URL http/https absoluta y canónica.
    /// Si no trae esquema se asume https:// ("www.google.com" → "https://www.google.com/").
    /// Rechaza otros esquemas (ftp:, mailto:, javascript:...), rutas relativas y hosts que no
    /// parecen un sitio web (sin punto, salvo localhost o una IP).
    /// </summary>
    public static bool TryNormalizeUrl(string? input, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        var candidate = input?.Trim();
        if (string.IsNullOrEmpty(candidate) || candidate.Length > MaxUrlLength)
        {
            return false;
        }

        if (candidate.StartsWith("//", StringComparison.Ordinal))
        {
            candidate = "https:" + candidate; // URL relativa al protocolo: //cdn.ejemplo.com/x
        }
        else if (!ExplicitSchemeRegex().IsMatch(candidate))
        {
            // "mailto:x", "javascript:x" o "http:ejemplo.com" llevan esquema pero no son web: no se les antepone nada.
            // "localhost:8080" o "ejemplo.com:443" no cuentan como esquema porque tras ':' viene un puerto.
            if (candidate.StartsWith('/') || NonWebSchemeRegex().IsMatch(candidate))
            {
                return false;
            }
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !HasWebHost(uri))
        {
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    private static bool HasWebHost(Uri uri) => uri.HostNameType switch
    {
        UriHostNameType.IPv4 or UriHostNameType.IPv6 => true,
        UriHostNameType.Dns => uri.IsLoopback
            || (uri.Host.Contains('.', StringComparison.Ordinal)
                && !uri.Host.StartsWith('.')
                && !uri.Host.EndsWith('.')),
        _ => false,
    };

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*://")]
    private static partial Regex ExplicitSchemeRegex();

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:(?!\d)")]
    private static partial Regex NonWebSchemeRegex();

    public async Task<UrlRecord> CreateAsync(string originalUrl, long userId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeUrl(originalUrl, out var normalizedUrl))
        {
            throw new ArgumentException(InvalidUrlMessage, nameof(originalUrl));
        }

        // Si el código aleatorio choca con uno existente, se reintenta con otro.
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var url = new UrlRecord(codes.Generate(), normalizedUrl, clock.GetUtcNow(), userId);
            if (await repository.TryInsertAsync(url, cancellationToken))
            {
                return url;
            }
        }

        throw new InvalidOperationException($"No se pudo generar un código único tras {MaxCodeAttempts} intentos.");
    }

    /// <summary>Crea la URL con un código elegido por el usuario.</summary>
    /// <returns>La URL creada, o <c>null</c> si el alias ya existe (de cualquier usuario).</returns>
    public async Task<UrlRecord?> TryCreateWithAliasAsync(
        string originalUrl, string alias, long userId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeUrl(originalUrl, out var normalizedUrl))
        {
            throw new ArgumentException(InvalidUrlMessage, nameof(originalUrl));
        }
        if (GetAliasError(alias) is { } aliasError)
        {
            throw new ArgumentException(aliasError, nameof(alias));
        }

        var url = new UrlRecord(alias, normalizedUrl, clock.GetUtcNow(), userId);
        return await repository.TryInsertAsync(url, cancellationToken) ? url : null;
    }

    /// <summary>Página de URLs del usuario (más recientes primero) con sus estadísticas.</summary>
    public async Task<PagedResult<UserUrlRecord>> ListMineAsync(
        long userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        var (items, total) = await repository.ListByUserAsync(userId, pageSize, (page - 1) * pageSize, cancellationToken);
        return new PagedResult<UserUrlRecord>(items, page, pageSize, total);
    }

    public Task<UrlRecord?> ResolveAsync(string code, CancellationToken cancellationToken = default) =>
        repository.FindByCodeAsync(code, cancellationToken);

    /// <summary>
    /// Resuelve un código corto y registra la visita (click + contadores) con la hora actual.
    /// Devuelve <c>null</c> si el código no existe; en ese caso no se registra nada.
    /// </summary>
    public Task<UrlRecord?> VisitAsync(string code, string? referrer, CancellationToken cancellationToken = default) =>
        repository.RecordVisitAsync(code, clock.GetUtcNow(), TryGetReferrerHost(referrer), cancellationToken);

    /// <summary>
    /// Extrae solo el host de un header Referer ("https://Twitter.com/x?y" → "twitter.com").
    /// Devuelve <c>null</c> si no hay Referer o no es una URL http/https válida.
    /// </summary>
    public static string? TryGetReferrerHost(string? referrer) =>
        Uri.TryCreate(referrer?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.Host.Length > 0
            ? uri.IdnHost.ToLowerInvariant()
            : null;

    public Task<IReadOnlyList<UrlRecord>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListAsync(cancellationToken);

    public async Task<DeleteUrlResult> DeleteAsync(string code, long userId, CancellationToken cancellationToken = default)
    {
        if (await repository.DeleteOwnedAsync(code, userId, cancellationToken))
        {
            return DeleteUrlResult.Deleted;
        }

        // No se borró: o no existe, o es de otro usuario.
        return await repository.FindByCodeAsync(code, cancellationToken) is null
            ? DeleteUrlResult.NotFound
            : DeleteUrlResult.NotOwner;
    }
}
