namespace Snap.Modules.Dashboard;

/// <summary>
/// Reglas del dashboard: calcula la ventana de tiempo, valida el periodo, rellena los días sin
/// clicks y calcula la variación. No sabe nada de HTTP.
/// </summary>
public sealed class DashboardService(DashboardRepository repository, TimeProvider clock)
{
    public const int DefaultDays = 30;
    public const int TopUrlsLimit = 20;
    public static readonly IReadOnlyList<int> AllowedDays = [7, 30, 90];
    public const string InvalidDaysMessage = "El periodo debe ser 7, 30 o 90 días.";

    public static bool IsValidPeriod(int days) => AllowedDays.Contains(days);

    /// <summary>
    /// Periodo móvil: el actual es (ahora − días, ahora] y el anterior tiene la misma duración justo antes,
    /// así la comparación es justa a cualquier hora del día. La serie diaria usa días naturales UTC
    /// (los últimos <paramref name="days"/>, incluido hoy).
    /// </summary>
    public async Task<DashboardResult> GetAsync(long userId, int days, CancellationToken cancellationToken = default)
    {
        if (!IsValidPeriod(days))
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, InvalidDaysMessage);
        }

        var now = clock.GetUtcNow();
        var length = TimeSpan.FromDays(days);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var window = new DashboardWindow(
            Now: now,
            From: now - length,
            PreviousFrom: now - length - length,
            FirstDay: today.AddDays(-(days - 1)),
            TopLimit: TopUrlsLimit);

        var stats = await repository.GetStatsAsync(userId, window, cancellationToken);

        var daily = Enumerable.Range(0, days)
            .Select(offset => window.FirstDay.AddDays(offset))
            .Select(day => new DailyClicks(day, stats.ClicksByDay.GetValueOrDefault(day)))
            .ToArray();

        var period = new DashboardPeriod(
            Days: days,
            From: window.From,
            To: window.Now,
            Clicks: stats.PeriodClicks,
            PreviousClicks: stats.PreviousPeriodClicks,
            ChangePercent: ChangePercent(stats.PeriodClicks, stats.PreviousPeriodClicks));

        return new DashboardResult(stats.Totals, period, daily, stats.TopUrls);
    }

    /// <summary>Variación porcentual con un decimal; null si no hay base (periodo anterior sin clicks).</summary>
    public static double? ChangePercent(long current, long previous) =>
        previous == 0 ? null : Math.Round((current - previous) * 100.0 / previous, 1, MidpointRounding.AwayFromZero);
}
