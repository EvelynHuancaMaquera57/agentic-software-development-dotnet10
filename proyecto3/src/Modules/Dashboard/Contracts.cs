namespace Snap.Modules.Dashboard;

// ---------- datos internos (repositorio ↔ servicio) ----------

/// <summary>Ventana de tiempo ya calculada que el repositorio usa como parámetros.</summary>
/// <param name="From">Inicio exclusivo del periodo actual: (From, Now].</param>
/// <param name="PreviousFrom">Inicio exclusivo del periodo anterior: (PreviousFrom, From].</param>
/// <param name="FirstDay">Primer día (UTC) de la serie diaria; el último es el día de <paramref name="Now"/>.</param>
public sealed record DashboardWindow(
    DateTimeOffset Now, DateTimeOffset From, DateTimeOffset PreviousFrom, DateOnly FirstDay, int TopLimit);

/// <summary>Una URL con sus métricas, tal como sale de la base (sin la URL corta, que depende de HTTP).</summary>
public sealed record DashboardUrlStats(
    string Code,
    string OriginalUrl,
    DateTimeOffset CreatedAt,
    long TotalClicks,
    long PeriodClicks,
    long PreviousPeriodClicks,
    DateTimeOffset? LastClickAt);

/// <summary>Todo lo que el repositorio lee en una misma transacción (foto consistente).</summary>
public sealed record DashboardStats(
    DashboardTotals Totals,
    long PeriodClicks,
    long PreviousPeriodClicks,
    IReadOnlyDictionary<DateOnly, long> ClicksByDay,
    IReadOnlyList<DashboardUrlStats> TopUrls);

/// <summary>Resultado del servicio, listo para convertirse en respuesta HTTP.</summary>
public sealed record DashboardResult(
    DashboardTotals Totals,
    DashboardPeriod Period,
    IReadOnlyList<DailyClicks> Daily,
    IReadOnlyList<DashboardUrlStats> Urls);

// ---------- respuesta HTTP: GET /api/dashboard ----------

public sealed record DashboardResponse(
    DashboardTotals Totals,
    DashboardPeriod Period,
    IReadOnlyList<DailyClicks> Daily,
    IReadOnlyList<DashboardUrl> Urls)
{
    public static DashboardResponse From(DashboardResult result, HttpRequest request) => new(
        result.Totals,
        result.Period,
        result.Daily,
        result.Urls.Select(url => DashboardUrl.From(url, request)).ToArray());
}

/// <summary>Totales de todo el historial del usuario.</summary>
public sealed record DashboardTotals(long Urls, long Clicks, long UrlsWithoutClicks);

/// <summary>
/// Periodo móvil de <see cref="Days"/> días que termina ahora, comparado con el periodo anterior
/// de la misma duración. <see cref="ChangePercent"/> es null si el periodo anterior no tuvo clicks.
/// </summary>
public sealed record DashboardPeriod(
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    long Clicks,
    long PreviousClicks,
    double? ChangePercent);

/// <summary>Clicks de un día natural en UTC.</summary>
public sealed record DailyClicks(DateOnly Date, long Clicks);

public sealed record DashboardUrl(
    string Code,
    string OriginalUrl,
    string ShortUrl,
    DateTimeOffset CreatedAt,
    long TotalClicks,
    long PeriodClicks,
    long PreviousPeriodClicks,
    DateTimeOffset? LastClickAt)
{
    public static DashboardUrl From(DashboardUrlStats url, HttpRequest request) => new(
        url.Code,
        url.OriginalUrl,
        $"{request.Scheme}://{request.Host}{request.PathBase}/{url.Code}",
        url.CreatedAt,
        url.TotalClicks,
        url.PeriodClicks,
        url.PreviousPeriodClicks,
        url.LastClickAt);
}
