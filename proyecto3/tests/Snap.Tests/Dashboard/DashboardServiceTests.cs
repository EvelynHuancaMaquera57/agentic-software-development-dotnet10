using Snap.Modules.Auth;
using Snap.Modules.Dashboard;
using Snap.Modules.Urls;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Dashboard;

/// <summary>
/// Reglas del dashboard con SQLite real y reloj fijo. "Ahora" es a las 09:00 UTC a propósito:
/// a media mañana es cuando un periodo por días naturales compararía un día a medias con días completos.
/// </summary>
public sealed class DashboardServiceTests : IAsyncLifetime, IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly TestDatabaseFile _database = new();
    private readonly FixedTimeProvider _clock = new(Now);
    private UrlsRepository _urls = null!;
    private DashboardService _dashboard = null!;
    private long _ana;
    private long _beto;

    public async Task InitializeAsync()
    {
        var connections = await _database.CreateInitializedAsync();
        _urls = new UrlsRepository(connections);
        _dashboard = new DashboardService(new DashboardRepository(connections), _clock);

        var users = new UsersRepository(connections);
        _ana = (await users.TryInsertAsync("ana@example.com", "hash", "Ana", Now))!.Value;
        _beto = (await users.TryInsertAsync("beto@example.com", "hash", "Beto", Now))!.Value;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _database.Dispose();

    // ---------- usuario sin URLs ----------

    [Fact]
    public async Task UserWithoutUrls_ReturnsZerosAndAFullSeriesOfEmptyDays()
    {
        // Otro usuario con actividad no debe filtrarse.
        await CreateUrlAsync("beto001", _beto);
        await ClickAsync("beto001", Now.AddHours(-1));

        var result = await _dashboard.GetAsync(_ana, days: 30);

        Assert.Equal(new DashboardTotals(Urls: 0, Clicks: 0, UrlsWithoutClicks: 0), result.Totals);
        Assert.Equal(0, result.Period.Clicks);
        Assert.Equal(0, result.Period.PreviousClicks);
        Assert.Null(result.Period.ChangePercent);
        Assert.Equal(30, result.Daily.Count);
        Assert.All(result.Daily, day => Assert.Equal(0, day.Clicks));
        Assert.Equal(new DateOnly(2026, 10, 20), result.Daily[0].Date);
        Assert.Equal(new DateOnly(2026, 11, 18), result.Daily[^1].Date);
        Assert.Empty(result.Urls);
    }

    // ---------- usuario con URLs y clicks ----------

    [Fact]
    public async Task UserWithUrlsAndClicks_ReturnsCorrectTotalsPeriodSeriesAndUrls()
    {
        await CreateUrlAsync("ana0001", _ana);
        await CreateUrlAsync("ana0002", _ana);
        await CreateUrlAsync("ana0003", _ana); // nunca visitada
        await CreateUrlAsync("beto001", _beto);

        // Periodo de 7 días: actual (11-11 09:00, 11-18 09:00], anterior (11-04 09:00, 11-11 09:00].
        // Se registran en orden cronológico, como ocurre en la realidad.
        await ClickAsync("ana0001", At(10, 1, 0));   // fuera de ambos periodos: solo cuenta en totales
        await ClickAsync("ana0001", At(11, 5, 0));   // anterior
        await ClickAsync("ana0001", At(11, 10, 12)); // anterior
        await ClickAsync("ana0001", At(11, 12, 0, 30)); // actual
        await ClickAsync("ana0001", At(11, 17, 10)); // actual
        await ClickAsync("ana0002", At(11, 18, 7));  // actual
        await ClickAsync("ana0001", At(11, 18, 8));  // actual
        await ClickAsync("beto001", At(11, 18, 8, 30)); // de otro usuario: no cuenta

        var result = await _dashboard.GetAsync(_ana, days: 7);

        Assert.Equal(new DashboardTotals(Urls: 3, Clicks: 7, UrlsWithoutClicks: 1), result.Totals);
        Assert.Equal(new DashboardPeriod(7, Now.AddDays(-7), Now, Clicks: 4, PreviousClicks: 2, ChangePercent: 100.0), result.Period);

        Assert.Equal(
            [
                new DailyClicks(new DateOnly(2026, 11, 12), 1),
                new DailyClicks(new DateOnly(2026, 11, 13), 0),
                new DailyClicks(new DateOnly(2026, 11, 14), 0),
                new DailyClicks(new DateOnly(2026, 11, 15), 0),
                new DailyClicks(new DateOnly(2026, 11, 16), 0),
                new DailyClicks(new DateOnly(2026, 11, 17), 1),
                new DailyClicks(new DateOnly(2026, 11, 18), 2),
            ],
            result.Daily);

        Assert.Equal(["ana0001", "ana0002", "ana0003"], result.Urls.Select(url => url.Code));
        var top = result.Urls[0];
        Assert.Equal((6L, 3L, 2L), (top.TotalClicks, top.PeriodClicks, top.PreviousPeriodClicks));
        Assert.Equal(At(11, 18, 8), top.LastClickAt);
        Assert.Equal("https://example.com/ana0001", top.OriginalUrl);
        var unvisited = result.Urls[2];
        Assert.Equal((0L, 0L, 0L), (unvisited.TotalClicks, unvisited.PeriodClicks, unvisited.PreviousPeriodClicks));
        Assert.Null(unvisited.LastClickAt);
    }

    [Fact]
    public async Task Periods_AreRollingAndHaveExclusiveStartAndInclusiveEnd()
    {
        await CreateUrlAsync("ana0001", _ana);
        var from = Now.AddDays(-7);
        var previousFrom = Now.AddDays(-14);

        await ClickAsync("ana0001", previousFrom);                  // fuera (inicio exclusivo del anterior)
        await ClickAsync("ana0001", previousFrom.AddSeconds(1));    // anterior
        await ClickAsync("ana0001", from);                          // anterior (frontera)
        await ClickAsync("ana0001", from.AddSeconds(1));            // actual
        await ClickAsync("ana0001", Now);                           // actual (fin inclusivo)
        await ClickAsync("ana0001", Now.AddSeconds(1));             // futuro: fuera

        var result = await _dashboard.GetAsync(_ana, days: 7);

        Assert.Equal(2, result.Period.Clicks);
        Assert.Equal(2, result.Period.PreviousClicks);
        Assert.Equal(6, result.Totals.Clicks);
    }

    [Fact]
    public async Task Urls_AreLimitedToTop20ButTotalsCountAll()
    {
        for (var i = 1; i <= 25; i++)
        {
            await CreateUrlAsync($"url{i:0000}", _ana);
        }
        await ClickAsync("url0003", Now.AddHours(-2));
        await ClickAsync("url0003", Now.AddHours(-1));
        await ClickAsync("url0010", Now.AddHours(-1));

        var result = await _dashboard.GetAsync(_ana, days: 30);

        Assert.Equal(25, result.Totals.Urls);
        Assert.Equal(23, result.Totals.UrlsWithoutClicks);
        Assert.Equal(DashboardService.TopUrlsLimit, result.Urls.Count);
        Assert.Equal(["url0003", "url0010", "url0025"], result.Urls.Take(3).Select(url => url.Code)); // luego, las más nuevas
    }

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task Daily_HasOneEntryPerDayEndingToday(int days)
    {
        var result = await _dashboard.GetAsync(_ana, days);

        Assert.Equal(days, result.Daily.Count);
        Assert.Equal(new DateOnly(2026, 11, 18), result.Daily[^1].Date);
        Assert.Equal(result.Daily.Select(d => d.Date).Order(), result.Daily.Select(d => d.Date));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(365)]
    [InlineData(-7)]
    public async Task GetAsync_InvalidPeriod_Throws(int days)
    {
        Assert.False(DashboardService.IsValidPeriod(days));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _dashboard.GetAsync(_ana, days));
    }

    [Theory]
    [InlineData(10, 0, null)]
    [InlineData(0, 0, null)]
    [InlineData(4, 2, 100.0)]
    [InlineData(5, 10, -50.0)]
    [InlineData(1, 3, -66.7)]
    [InlineData(3, 3, 0.0)]
    public void ChangePercent_IsRoundedAndNullWithoutBase(long current, long previous, double? expected)
    {
        Assert.Equal(expected, DashboardService.ChangePercent(current, previous));
    }

    private static DateTimeOffset At(int month, int day, int hour, int minute = 0) =>
        new(2026, month, day, hour, minute, 0, TimeSpan.Zero);

    private async Task CreateUrlAsync(string code, long userId) =>
        Assert.True(await _urls.TryInsertAsync(new UrlRecord(code, $"https://example.com/{code}", Now.AddDays(-60), userId)));

    private async Task ClickAsync(string code, DateTimeOffset at) =>
        Assert.NotNull(await _urls.RecordVisitAsync(code, at, referrerHost: null));
}
