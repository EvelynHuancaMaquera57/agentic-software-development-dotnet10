using Microsoft.Data.Sqlite;

namespace Snap.Data;

/// <summary>
/// Aplica, en orden alfabético, los scripts embebidos en Data/Scripts que aún no se hayan aplicado.
/// Cada script se ejecuta una sola vez (registrado en <c>schema_migrations</c>) y dentro de una
/// transacción, así que pueden contener sentencias no idempotentes como ALTER TABLE.
/// </summary>
public static class DatabaseInitializer
{
    private const string ScriptPrefix = "Snap.Data.Scripts.";

    public static async Task InitializeAsync(SqliteConnectionFactory connections, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        var scripts = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ScriptPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        await using var connection = await connections.OpenAsync(cancellationToken);

        // WAL: lectores y escritor no se bloquean entre sí (el dashboard no frena los redirects que
        // registran clicks). Es persistente: queda guardado en el archivo de la base.
        await ExecuteAsync(connection, null, "PRAGMA journal_mode = WAL;", cancellationToken);

        await ExecuteAsync(connection, null, """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                name       TEXT PRIMARY KEY,
                applied_at TEXT NOT NULL
            );
            """, cancellationToken);
        var applied = await GetAppliedAsync(connection, cancellationToken);

        foreach (var resource in scripts)
        {
            var name = resource[ScriptPrefix.Length..];
            if (applied.Contains(name))
            {
                continue;
            }

            await using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"No se encontró el script embebido '{resource}'.");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(connection, transaction, sql, cancellationToken);
            await using (var record = connection.CreateCommand())
            {
                record.Transaction = transaction;
                record.CommandText = "INSERT INTO schema_migrations (name, applied_at) VALUES ($name, $appliedAt);";
                record.Parameters.AddWithValue("$name", name);
                record.Parameters.AddWithValue("$appliedAt", SqliteTime.Format(DateTimeOffset.UtcNow));
                await record.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task<HashSet<string>> GetAppliedAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM schema_migrations;";
        var names = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
