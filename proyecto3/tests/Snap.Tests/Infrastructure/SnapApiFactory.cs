using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Snap.Tests.Infrastructure;

/// <summary>Levanta la API en memoria apuntando a una base SQLite temporal y exclusiva.</summary>
public sealed class SnapApiFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabaseFile _database = new();

    public const string TestSigningKey = "test-signing-key-0123456789-abcdefghijklmnop";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Snap", _database.ConnectionString);
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _database.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _database.Dispose();
    }
}
