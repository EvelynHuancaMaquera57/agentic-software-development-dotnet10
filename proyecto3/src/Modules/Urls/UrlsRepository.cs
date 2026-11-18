using Microsoft.Data.Sqlite;
using Snap.Data;

namespace Snap.Modules.Urls;

/// <summary>Único punto con SQL del módulo. Consultas parametrizadas, sin ORM.</summary>
public sealed class UrlsRepository(SqliteConnectionFactory connections)
{
    private const int SqliteConstraintUnique = 2067;
    private const string SelectColumns = "SELECT code, original_url, created_at, user_id FROM urls";

    /// <returns><c>false</c> si el código ya existe (violación de UNIQUE).</returns>
    public async Task<bool> TryInsertAsync(UrlRecord url, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO urls (code, original_url, created_at, user_id)
            VALUES ($code, $originalUrl, $createdAt, $userId);
            """;
        command.Parameters.AddWithValue("$code", url.Code);
        command.Parameters.AddWithValue("$originalUrl", url.OriginalUrl);
        command.Parameters.AddWithValue("$createdAt", SqliteTime.Format(url.CreatedAt));
        command.Parameters.AddWithValue("$userId", (object?)url.UserId ?? DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteConstraintUnique)
        {
            return false;
        }
    }

    public async Task<UrlRecord?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE code = $code;";
        command.Parameters.AddWithValue("$code", code);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    /// <summary>Todas las URLs, de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<UrlRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} ORDER BY id DESC;";

        var urls = new List<UrlRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            urls.Add(Map(reader));
        }
        return urls;
    }

    /// <summary>
    /// URLs de un usuario, de la más reciente a la más antigua, con sus contadores, más el total.
    /// Usa ix_urls_user_id (que incluye el rowid, así que también sirve para ordenar por id).
    /// </summary>
    public async Task<(IReadOnlyList<UserUrlRecord> Items, long Total)> ListByUserAsync(
        long userId, int limit, int offset, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);

        // Lectura diferida: COUNT y página salen de la misma foto sin tomar el lock de escritura.
        await using var transaction = connection.BeginTransaction(deferred: true);

        long total;
        await using (var count = connection.CreateCommand())
        {
            count.Transaction = transaction;
            count.CommandText = "SELECT COUNT(*) FROM urls WHERE user_id = $userId;";
            count.Parameters.AddWithValue("$userId", userId);
            total = (long)(await count.ExecuteScalarAsync(cancellationToken))!;
        }

        var items = new List<UserUrlRecord>();
        await using (var page = connection.CreateCommand())
        {
            page.Transaction = transaction;
            page.CommandText = """
                SELECT code, original_url, created_at, total_clicks, last_click_at
                FROM urls
                WHERE user_id = $userId
                ORDER BY id DESC
                LIMIT $limit OFFSET $offset;
                """;
            page.Parameters.AddWithValue("$userId", userId);
            page.Parameters.AddWithValue("$limit", limit);
            page.Parameters.AddWithValue("$offset", offset);

            await using var reader = await page.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new UserUrlRecord(
                    Code: reader.GetString(0),
                    OriginalUrl: reader.GetString(1),
                    CreatedAt: SqliteTime.Parse(reader.GetString(2)),
                    TotalClicks: reader.GetInt64(3),
                    LastClickAt: reader.IsDBNull(4) ? null : SqliteTime.Parse(reader.GetString(4))));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return (items, total);
    }

    /// <summary>
    /// Borra la URL solo si pertenece a <paramref name="userId"/>. La comprobación de dueño va en el
    /// propio DELETE, así no hay ventana entre "comprobar" y "borrar".
    /// </summary>
    /// <returns><c>true</c> si se borró una fila.</returns>
    public async Task<bool> DeleteOwnedAsync(string code, long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM urls WHERE code = $code AND user_id = $userId;";
        command.Parameters.AddWithValue("$code", code);
        command.Parameters.AddWithValue("$userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <summary>
    /// Registra una visita en una sola transacción: incrementa los contadores de la URL y guarda el click.
    /// El UPDATE ... RETURNING hace a la vez de búsqueda, así que el redirect sigue siendo un solo viaje.
    /// </summary>
    /// <returns>La URL visitada, o <c>null</c> si el código no existe (y entonces no se registra nada).</returns>
    public async Task<UrlRecord?> RecordVisitAsync(
        string code, DateTimeOffset clickedAt, string? referrerHost, CancellationToken cancellationToken = default)
    {
        var clickedAtText = SqliteTime.Format(clickedAt);
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        long urlId;
        UrlRecord url;
        await using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE urls SET total_clicks = total_clicks + 1, last_click_at = $clickedAt
                WHERE code = $code
                RETURNING code, original_url, created_at, user_id, id;
                """;
            update.Parameters.AddWithValue("$code", code);
            update.Parameters.AddWithValue("$clickedAt", clickedAtText);

            await using var reader = await update.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null; // el using hace rollback de la transacción vacía
            }
            url = Map(reader);
            urlId = reader.GetInt64(4);
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO clicks (url_id, clicked_at, referrer_host)
                VALUES ($urlId, $clickedAt, $referrerHost);
                """;
            insert.Parameters.AddWithValue("$urlId", urlId);
            insert.Parameters.AddWithValue("$clickedAt", clickedAtText);
            insert.Parameters.AddWithValue("$referrerHost", (object?)referrerHost ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return url;
    }

    private static UrlRecord Map(SqliteDataReader reader) => new(
        Code: reader.GetString(0),
        OriginalUrl: reader.GetString(1),
        CreatedAt: SqliteTime.Parse(reader.GetString(2)),
        UserId: reader.IsDBNull(3) ? null : reader.GetInt64(3));
}
