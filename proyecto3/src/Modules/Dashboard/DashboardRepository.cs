using System.Globalization;
using Microsoft.Data.Sqlite;
using Snap.Data;

namespace Snap.Modules.Dashboard;

/// <summary>
/// Único punto con SQL del módulo. Solo lee: los clicks los escribe el módulo Urls al redirigir.
/// Las cuatro consultas usan ix_urls_user_id e ix_clicks_url_time (índice cubriente por url y fecha).
/// </summary>
public sealed class DashboardRepository(SqliteConnectionFactory connections)
{
    public async Task<DashboardStats> GetStatsAsync(long userId, DashboardWindow window, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);

        // Transacción de solo lectura (diferida: no toma el lock de escritura). Con WAL da una foto
        // consistente de las cuatro consultas sin bloquear a los redirects que registran clicks.
        await using var transaction = connection.BeginTransaction(deferred: true);

        var parameters = new Dictionary<string, object>
        {
            ["$userId"] = userId,
            ["$now"] = SqliteTime.Format(window.Now),
            ["$from"] = SqliteTime.Format(window.From),
            ["$previousFrom"] = SqliteTime.Format(window.PreviousFrom),
            ["$firstDay"] = SqliteTime.Format(new DateTimeOffset(window.FirstDay, TimeOnly.MinValue, TimeSpan.Zero)),
            ["$limit"] = window.TopLimit,
        };

        // 1. Totales históricos: salen de los contadores de urls, sin recorrer clicks.
        var totals = await QuerySingleAsync(connection, transaction, """
            SELECT COUNT(*),
                   COALESCE(SUM(total_clicks), 0),
                   COALESCE(SUM(total_clicks = 0), 0)
            FROM urls
            WHERE user_id = $userId;
            """, parameters, reader => new DashboardTotals(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2)),
            cancellationToken);

        // 2. Periodo actual (from, now] y anterior (previousFrom, from] en una sola pasada por el índice.
        var (periodClicks, previousClicks) = await QuerySingleAsync(connection, transaction, """
            SELECT COALESCE(SUM(c.clicked_at > $from), 0),
                   COALESCE(SUM(c.clicked_at <= $from), 0)
            FROM urls u
            JOIN clicks c ON c.url_id = u.id
            WHERE u.user_id = $userId
              AND c.clicked_at > $previousFrom AND c.clicked_at <= $now;
            """, parameters, reader => (reader.GetInt64(0), reader.GetInt64(1)), cancellationToken);

        // 3. Serie diaria. Las fechas están en UTC con formato fijo, así que los 10 primeros
        //    caracteres son el día ("2026-11-18"); los días sin clicks los rellena el servicio.
        var clicksByDay = new Dictionary<DateOnly, long>();
        await QueryAsync(connection, transaction, """
            SELECT substr(c.clicked_at, 1, 10) AS day, COUNT(*)
            FROM urls u
            JOIN clicks c ON c.url_id = u.id
            WHERE u.user_id = $userId
              AND c.clicked_at >= $firstDay AND c.clicked_at <= $now
            GROUP BY day;
            """, parameters, reader => clicksByDay[DateOnly.ParseExact(reader.GetString(0), "yyyy-MM-dd", CultureInfo.InvariantCulture)] = reader.GetInt64(1),
            cancellationToken);

        // 4. Top URLs del periodo. El JOIN solo trae clicks de la ventana; los totales vienen de los contadores.
        var topUrls = new List<DashboardUrlStats>();
        await QueryAsync(connection, transaction, """
            SELECT u.code, u.original_url, u.created_at, u.total_clicks, u.last_click_at,
                   COALESCE(SUM(c.clicked_at > $from), 0)  AS period_clicks,
                   COALESCE(SUM(c.clicked_at <= $from), 0) AS previous_clicks
            FROM urls u
            LEFT JOIN clicks c ON c.url_id = u.id
                              AND c.clicked_at > $previousFrom AND c.clicked_at <= $now
            WHERE u.user_id = $userId
            GROUP BY u.id
            ORDER BY period_clicks DESC, u.total_clicks DESC, u.id DESC
            LIMIT $limit;
            """, parameters, reader => topUrls.Add(new DashboardUrlStats(
                Code: reader.GetString(0),
                OriginalUrl: reader.GetString(1),
                CreatedAt: SqliteTime.Parse(reader.GetString(2)),
                TotalClicks: reader.GetInt64(3),
                PeriodClicks: reader.GetInt64(5),
                PreviousPeriodClicks: reader.GetInt64(6),
                LastClickAt: reader.IsDBNull(4) ? null : SqliteTime.Parse(reader.GetString(4)))),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new DashboardStats(totals, periodClicks, previousClicks, clicksByDay, topUrls);
    }

    private static async Task<T> QuerySingleAsync<T>(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        IReadOnlyDictionary<string, object> parameters, Func<SqliteDataReader, T> map, CancellationToken cancellationToken)
    {
        T? result = default;
        var found = false;
        await QueryAsync(connection, transaction, sql, parameters, reader =>
        {
            result = map(reader);
            found = true;
        }, cancellationToken);
        return found ? result! : throw new InvalidOperationException("La consulta de agregación no devolvió filas.");
    }

    private static async Task QueryAsync(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        IReadOnlyDictionary<string, object> parameters, Action<SqliteDataReader> onRow, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            // Microsoft.Data.Sqlite enlaza por nombre e ignora los que la consulta no usa.
            command.Parameters.AddWithValue(name, value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            onRow(reader);
        }
    }
}
