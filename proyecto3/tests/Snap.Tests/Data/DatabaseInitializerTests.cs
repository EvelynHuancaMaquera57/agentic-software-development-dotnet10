using Microsoft.Data.Sqlite;
using Snap.Data;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Data;

public sealed class DatabaseInitializerTests : IDisposable
{
    private static readonly string[] AllScripts = [
        "001_create_urls.sql", "002_create_users.sql", "003_add_urls_user_id.sql",
        "004_create_clicks.sql", "005_add_url_click_counters.sql",
    ];

    private readonly TestDatabaseFile _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Initialize_CreatesAllTables()
    {
        var connections = await _database.CreateInitializedAsync();

        var tables = await QueryColumnAsync(connections,
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;");

        Assert.Equal(["clicks", "schema_migrations", "urls", "users"], tables);
    }

    [Fact]
    public async Task Initialize_UsersTableHasExpectedColumns()
    {
        var connections = await _database.CreateInitializedAsync();

        var columns = await QueryColumnAsync(connections, "SELECT name FROM pragma_table_info('users') ORDER BY cid;");

        Assert.Equal(["id", "email", "password_hash", "name", "created_at"], columns);
    }

    [Fact]
    public async Task Initialize_UrlsTableHasOwnerColumnReferencingUsers()
    {
        var connections = await _database.CreateInitializedAsync();

        var columns = await QueryColumnAsync(connections, "SELECT name FROM pragma_table_info('urls') ORDER BY cid;");
        var references = await QueryColumnAsync(connections, "SELECT \"table\" || '.' || \"to\" FROM pragma_foreign_key_list('urls');");

        Assert.Equal(["id", "code", "original_url", "created_at", "user_id", "total_clicks", "last_click_at"], columns);
        Assert.Equal(["users.id"], references);
    }

    [Fact]
    public async Task Initialize_ClicksTableReferencesUrlsWithCascadeAndCoveringIndex()
    {
        var connections = await _database.CreateInitializedAsync();

        var columns = await QueryColumnAsync(connections, "SELECT name FROM pragma_table_info('clicks') ORDER BY cid;");
        var references = await QueryColumnAsync(connections,
            "SELECT \"table\" || '.' || \"to\" || ' ' || on_delete FROM pragma_foreign_key_list('clicks');");
        var indexColumns = await QueryColumnAsync(connections, "SELECT name FROM pragma_index_info('ix_clicks_url_time') ORDER BY seqno;");

        Assert.Equal(["id", "url_id", "clicked_at", "referrer_host"], columns);
        Assert.Equal(["urls.id CASCADE"], references);
        Assert.Equal(["url_id", "clicked_at"], indexColumns);
    }

    [Fact]
    public async Task DeletingUrl_DeletesItsClicks()
    {
        var connections = await _database.CreateInitializedAsync();
        await ExecuteAsync(connections, """
            INSERT INTO urls (code, original_url, created_at) VALUES ('abc1234', 'https://example.com', '2026-11-18T00:00:00.0000000+00:00');
            INSERT INTO clicks (url_id, clicked_at) VALUES (1, '2026-11-18T01:00:00.0000000+00:00'), (1, '2026-11-18T02:00:00.0000000+00:00');
            """);

        await ExecuteAsync(connections, "DELETE FROM urls WHERE code = 'abc1234';");

        Assert.Equal(["0"], await QueryColumnAsync(connections, "SELECT CAST(COUNT(*) AS TEXT) FROM clicks;"));
    }

    [Fact]
    public async Task Initialize_EnablesWalAndConnectionsUseSynchronousNormal()
    {
        var connections = await _database.CreateInitializedAsync();

        Assert.Equal(["wal"], await QueryColumnAsync(connections, "PRAGMA journal_mode;"));
        Assert.Equal(["1"], await QueryColumnAsync(connections, "SELECT CAST(synchronous AS TEXT) FROM pragma_synchronous;")); // 1 = NORMAL
    }

    [Fact]
    public async Task Initialize_RecordsEveryScriptAsApplied()
    {
        var connections = await _database.CreateInitializedAsync();

        var applied = await QueryColumnAsync(connections, "SELECT name FROM schema_migrations ORDER BY name;");

        Assert.Equal(AllScripts, applied);
    }

    [Fact]
    public async Task Initialize_RunTwice_DoesNotReapplyScripts()
    {
        // 003 contiene un ALTER TABLE: si se re-ejecutara fallaría con "duplicate column name".
        var connections = await _database.CreateInitializedAsync();

        await DatabaseInitializer.InitializeAsync(connections);

        Assert.Equal(AllScripts, await QueryColumnAsync(connections, "SELECT name FROM schema_migrations ORDER BY name;"));
    }

    [Fact]
    public async Task Initialize_UpgradesDatabaseCreatedBeforeMigrationsTable()
    {
        // Simula el snap.db de antes de este cambio: urls sin user_id, con datos, y sin schema_migrations.
        var connections = new SqliteConnectionFactory(_database.ConnectionString);
        await ExecuteAsync(connections, """
            CREATE TABLE urls (
                id INTEGER PRIMARY KEY AUTOINCREMENT, code TEXT NOT NULL UNIQUE,
                original_url TEXT NOT NULL, created_at TEXT NOT NULL);
            INSERT INTO urls (code, original_url, created_at) VALUES ('old0001', 'https://example.com', '2026-11-17T00:00:00Z');
            """);

        await DatabaseInitializer.InitializeAsync(connections);

        Assert.Equal(["old0001"], await QueryColumnAsync(connections, "SELECT code FROM urls WHERE user_id IS NULL;"));
        Assert.Equal(AllScripts, await QueryColumnAsync(connections, "SELECT name FROM schema_migrations ORDER BY name;"));
    }

    [Fact]
    public async Task Users_EmailIsUniqueIgnoringCase()
    {
        var connections = await _database.CreateInitializedAsync();
        await InsertUserAsync(connections, "ana@example.com");

        var ex = await Assert.ThrowsAsync<SqliteException>(() => InsertUserAsync(connections, "ANA@Example.com"));

        Assert.Equal(2067, ex.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_UNIQUE
    }

    private static Task InsertUserAsync(SqliteConnectionFactory connections, string email) =>
        ExecuteAsync(connections, """
            INSERT INTO users (email, password_hash, name, created_at)
            VALUES ($email, 'hash', 'Ana', '2026-11-18T00:00:00.0000000+00:00');
            """, ("$email", email));

    private static async Task ExecuteAsync(SqliteConnectionFactory connections, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await connections.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryColumnAsync(SqliteConnectionFactory connections, string sql)
    {
        await using var connection = await connections.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }
        return values;
    }
}
