using Microsoft.Data.Sqlite;

namespace Snap.Data;

/// <summary>Abre conexiones SQLite a partir de la cadena de conexión configurada.</summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        // SQLite no aplica las claves foráneas (REFERENCES) salvo que se active en cada conexión.
        _connectionString = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true }.ToString();
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Con WAL (activado en DatabaseInitializer) NORMAL es seguro ante caídas de la app: un corte de
        // luz puede perder las últimas escrituras, nunca corromper la base. Multiplica la velocidad de
        // escritura de clicks. Es un ajuste por conexión, por eso va aquí.
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = NORMAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }
}
