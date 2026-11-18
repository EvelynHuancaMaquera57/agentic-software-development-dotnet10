using Snap.Data;

namespace Snap.Tests.Infrastructure;

/// <summary>
/// Archivo SQLite temporal que se borra al terminar. Sin pooling para que
/// ninguna conexión quede abierta y el archivo se pueda eliminar.
/// </summary>
public sealed class TestDatabaseFile : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"snap-tests-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_path};Pooling=False";

    public async Task<SqliteConnectionFactory> CreateInitializedAsync()
    {
        var connections = new SqliteConnectionFactory(ConnectionString);
        await DatabaseInitializer.InitializeAsync(connections);
        return connections;
    }

    public void Dispose()
    {
        // Con WAL SQLite crea además los archivos -wal y -shm junto a la base.
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
