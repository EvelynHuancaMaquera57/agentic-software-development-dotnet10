namespace Snap.Modules.Urls;

/// <summary>Fila de la tabla <c>urls</c>. <see cref="UserId"/> es null solo en URLs anteriores a la autenticación.</summary>
public sealed record UrlRecord(string Code, string OriginalUrl, DateTimeOffset CreatedAt, long? UserId);

/// <summary>Una URL del usuario con sus contadores de clicks (columnas desnormalizadas de <c>urls</c>).</summary>
public sealed record UserUrlRecord(
    string Code, string OriginalUrl, DateTimeOffset CreatedAt, long TotalClicks, DateTimeOffset? LastClickAt);

/// <summary>Una página de resultados más el total de elementos.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

public enum DeleteUrlResult
{
    Deleted,
    NotFound,
    NotOwner,
}

/// <param name="Url">URL larga; si no trae esquema se asume https://.</param>
/// <param name="Alias">Código corto personalizado (opcional). Vacío o null = código aleatorio.</param>
public sealed record CreateUrlRequest(string? Url, string? Alias = null);

public sealed record UrlResponse(string Code, string OriginalUrl, string ShortUrl, DateTimeOffset CreatedAt)
{
    public static UrlResponse From(UrlRecord url, HttpRequest request) => new(
        url.Code,
        url.OriginalUrl,
        ShortUrlFor(url.Code, request),
        url.CreatedAt);

    /// <summary>URL pública del código corto, en el mismo host por el que llegó la petición.</summary>
    public static string ShortUrlFor(string code, HttpRequest request) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}/{Uri.EscapeDataString(code)}";
}

/// <summary>Elemento de GET /api/urls/mine: la URL y sus estadísticas.</summary>
public sealed record MyUrlResponse(
    string Code,
    string OriginalUrl,
    string ShortUrl,
    DateTimeOffset CreatedAt,
    long TotalClicks,
    DateTimeOffset? LastClickAt)
{
    public static MyUrlResponse From(UserUrlRecord url, HttpRequest request) => new(
        url.Code,
        url.OriginalUrl,
        UrlResponse.ShortUrlFor(url.Code, request),
        url.CreatedAt,
        url.TotalClicks,
        url.LastClickAt);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
