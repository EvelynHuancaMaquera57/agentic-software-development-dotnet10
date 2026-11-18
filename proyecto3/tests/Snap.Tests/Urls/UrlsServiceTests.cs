using Microsoft.Data.Sqlite;
using Snap.Data;
using Snap.Modules.Auth;
using Snap.Modules.Urls;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Urls;

/// <summary>Lógica de negocio sin HTTP, con SQLite real y un generador de códigos controlado.</summary>
public sealed class UrlsServiceTests : IAsyncLifetime, IDisposable
{
    private readonly TestDatabaseFile _database = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
    private SqliteConnectionFactory _connections = null!;
    private UrlsRepository _repository = null!;
    private long _ana;
    private long _beto;

    public async Task InitializeAsync()
    {
        var connections = await _database.CreateInitializedAsync();
        _connections = connections;
        _repository = new UrlsRepository(connections);

        // Las URLs referencian users(id) con clave foránea, así que los dueños tienen que existir.
        var users = new UsersRepository(connections);
        _ana = (await users.TryInsertAsync("ana@example.com", "hash", "Ana", DateTimeOffset.UtcNow))!.Value;
        _beto = (await users.TryInsertAsync("beto@example.com", "hash", "Beto", DateTimeOffset.UtcNow))!.Value;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _database.Dispose();

    // ---------- crear ----------

    [Fact]
    public async Task CreateAsync_PersistsUrlWithOwnerAndCanBeResolved()
    {
        var service = CreateService("abc1234");

        var created = await service.CreateAsync("https://example.com", _ana);
        var resolved = await service.ResolveAsync("abc1234");

        Assert.Equal("abc1234", created.Code);
        Assert.Equal(_ana, created.UserId);
        Assert.Equal(created, resolved);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeCollides_RetriesWithNewCode()
    {
        var service = CreateService("dup0001", "dup0001", "new0002");

        await service.CreateAsync("https://example.com/first", _ana);
        var second = await service.CreateAsync("https://example.com/second", _ana);

        Assert.Equal("new0002", second.Code);
        Assert.Equal(2, (await service.ListAsync()).Count);
    }

    [Fact]
    public async Task CreateAsync_WhenAllAttemptsCollide_Throws()
    {
        var codes = Enumerable.Repeat("same000", UrlsService.MaxCodeAttempts + 1).ToArray();
        var service = CreateService(codes);
        await service.CreateAsync("https://example.com/first", _ana);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("https://example.com/second", _ana));
    }

    [Fact]
    public async Task CreateAsync_InvalidUrl_ThrowsAndDoesNotPersist()
    {
        var service = CreateService("abc1234");

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("ftp://example.com", _ana));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task CreateAsync_NonexistentUser_IsRejectedByForeignKey()
    {
        var service = CreateService("abc1234");

        var ex = await Assert.ThrowsAsync<SqliteException>(() => service.CreateAsync("https://example.com", userId: 999));

        Assert.Equal(787, ex.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_FOREIGNKEY
    }

    [Fact]
    public async Task ResolveAsync_UnknownCode_ReturnsNull()
    {
        var service = CreateService();

        Assert.Null(await service.ResolveAsync("missing"));
    }

    // ---------- borrar ----------

    [Fact]
    public async Task DeleteAsync_ByOwner_DeletesUrl()
    {
        var service = CreateService("abc1234");
        await service.CreateAsync("https://example.com", _ana);

        var result = await service.DeleteAsync("abc1234", _ana);

        Assert.Equal(DeleteUrlResult.Deleted, result);
        Assert.Null(await service.ResolveAsync("abc1234"));
    }

    [Fact]
    public async Task DeleteAsync_ByAnotherUser_ReturnsNotOwnerAndKeepsUrl()
    {
        var service = CreateService("abc1234");
        await service.CreateAsync("https://example.com", _ana);

        var result = await service.DeleteAsync("abc1234", _beto);

        Assert.Equal(DeleteUrlResult.NotOwner, result);
        Assert.NotNull(await service.ResolveAsync("abc1234"));
    }

    [Fact]
    public async Task DeleteAsync_UnknownCode_ReturnsNotFound()
    {
        var service = CreateService();

        Assert.Equal(DeleteUrlResult.NotFound, await service.DeleteAsync("missing", _ana));
    }

    [Fact]
    public async Task DeleteAsync_UrlWithoutOwner_CannotBeDeletedByAnyone()
    {
        // URLs creadas antes de la autenticación tienen user_id NULL.
        await _repository.TryInsertAsync(new UrlRecord("legacy1", "https://example.com", DateTimeOffset.UtcNow, UserId: null));
        var service = CreateService();

        Assert.Equal(DeleteUrlResult.NotOwner, await service.DeleteAsync("legacy1", _ana));
        Assert.NotNull(await service.ResolveAsync("legacy1"));
    }

    [Fact]
    public async Task CreateAsync_UrlWithoutScheme_IsStoredWithHttps()
    {
        var service = CreateService("abc1234");

        var created = await service.CreateAsync("www.google.com", _ana);

        Assert.Equal("https://www.google.com/", created.OriginalUrl);
        Assert.Equal("https://www.google.com/", (await service.ResolveAsync("abc1234"))!.OriginalUrl);
    }

    // ---------- alias ----------

    [Fact]
    public async Task TryCreateWithAliasAsync_FreeAlias_CreatesUrlWithThatCode()
    {
        var service = CreateService();

        var created = await service.TryCreateWithAliasAsync("www.google.com", "mi-google", _ana);

        Assert.Equal(new UrlRecord("mi-google", "https://www.google.com/", _clock.Now, _ana), created);
        Assert.Equal(created, await service.ResolveAsync("mi-google"));
    }

    [Fact]
    public async Task TryCreateWithAliasAsync_TakenAlias_ReturnsNullAndKeepsOriginal()
    {
        var service = CreateService();
        await service.TryCreateWithAliasAsync("https://example.com/primera", "promo", _ana);

        Assert.Null(await service.TryCreateWithAliasAsync("https://example.com/segunda", "promo", _beto));
        Assert.Equal("https://example.com/primera", (await service.ResolveAsync("promo"))!.OriginalUrl);
    }

    [Fact]
    public async Task TryCreateWithAliasAsync_InvalidAlias_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.TryCreateWithAliasAsync("https://example.com", "no válido", _ana));
        await Assert.ThrowsAsync<ArgumentException>(() => service.TryCreateWithAliasAsync("https://example.com", "health", _ana));
        Assert.Empty(await service.ListAsync());
    }

    [Theory]
    [InlineData("abc", null)]
    [InlineData("Mi-Enlace_2026", null)]
    [InlineData("123456789012345678901234567890", null)]
    [InlineData("ab", UrlsService.InvalidAliasMessage)]
    [InlineData("1234567890123456789012345678901", UrlsService.InvalidAliasMessage)]
    [InlineData("con espacio", UrlsService.InvalidAliasMessage)]
    [InlineData("acción", UrlsService.InvalidAliasMessage)]
    [InlineData("a.b", UrlsService.InvalidAliasMessage)]
    [InlineData("a/b", UrlsService.InvalidAliasMessage)]
    [InlineData("health", UrlsService.ReservedAliasMessage)]
    [InlineData("Dashboard", UrlsService.ReservedAliasMessage)]
    public void GetAliasError_ValidatesFormatAndReservedWords(string alias, string? expected)
    {
        Assert.Equal(expected, UrlsService.GetAliasError(alias));
    }

    // ---------- mis URLs ----------

    [Fact]
    public async Task ListMineAsync_ReturnsOwnUrlsNewestFirstWithCountersAndTotal()
    {
        var service = CreateService("ana0001", "beto001", "ana0002", "ana0003");
        await service.CreateAsync("https://example.com/1", _ana);
        await service.CreateAsync("https://example.com/beto", _beto);
        await service.CreateAsync("https://example.com/2", _ana);
        await service.CreateAsync("https://example.com/3", _ana);
        await service.VisitAsync("ana0002", referrer: null);

        var page = await service.ListMineAsync(_ana, page: 1, pageSize: 2);

        Assert.Equal(3, page.Total);
        Assert.Equal(["ana0003", "ana0002"], page.Items.Select(url => url.Code));
        Assert.Equal((1L, (DateTimeOffset?)_clock.Now), (page.Items[1].TotalClicks, page.Items[1].LastClickAt));
        Assert.Equal(["ana0001"], (await service.ListMineAsync(_ana, page: 2, pageSize: 2)).Items.Select(url => url.Code));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ListMineAsync_InvalidPaging_Throws(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateService().ListMineAsync(_ana, page, pageSize));
    }

    // ---------- visitas (clicks) ----------

    [Fact]
    public async Task VisitAsync_ExistingCode_ReturnsUrlRecordsClickAndUpdatesCounters()
    {
        var service = CreateService("abc1234");
        await service.CreateAsync("https://example.com", _ana);

        var first = await service.VisitAsync("abc1234", "https://Twitter.com/post/1?x=y");
        _clock.Now = _clock.Now.AddMinutes(5);
        var second = await service.VisitAsync("abc1234", referrer: null);

        Assert.Equal("https://example.com/", first?.OriginalUrl);
        Assert.Equal("abc1234", second?.Code);
        Assert.Equal(
            [("2026-11-18T12:00:00.0000000+00:00", "twitter.com"), ("2026-11-18T12:05:00.0000000+00:00", null)],
            await ReadClicksAsync());
        var (total, lastClickAt) = await ReadCountersAsync("abc1234");
        Assert.Equal(2, total);
        Assert.Equal("2026-11-18T12:05:00.0000000+00:00", lastClickAt);
    }

    [Fact]
    public async Task VisitAsync_UnknownCode_ReturnsNullAndRecordsNothing()
    {
        var service = CreateService();

        Assert.Null(await service.VisitAsync("missing", "https://twitter.com"));
        Assert.Empty(await ReadClicksAsync());
    }

    [Fact]
    public async Task VisitAsync_ManyVisits_CounterMatchesClicksTable()
    {
        var service = CreateService("aaa0001", "bbb0002");
        await service.CreateAsync("https://example.com/a", _ana);
        await service.CreateAsync("https://example.com/b", _beto);

        for (var i = 0; i < 7; i++)
        {
            await service.VisitAsync(i % 3 == 0 ? "bbb0002" : "aaa0001", referrer: null);
        }

        Assert.Equal((4L, (string?)"2026-11-18T12:00:00.0000000+00:00"), await ReadCountersAsync("aaa0001"));
        Assert.Equal(3, (await ReadCountersAsync("bbb0002")).Total);
        Assert.Equal(7, (await ReadClicksAsync()).Count);
    }

    [Theory]
    [InlineData("https://twitter.com/post/1", "twitter.com")]
    [InlineData("http://WWW.Google.COM/search?q=snap", "www.google.com")]
    [InlineData("  https://news.ycombinator.com/  ", "news.ycombinator.com")]
    [InlineData("https://localhost:5080/x", "localhost")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("no-es-una-url", null)]
    [InlineData("/ruta/relativa", null)]
    [InlineData("android-app://com.twitter.android", null)]
    [InlineData("javascript:alert(1)", null)]
    public void TryGetReferrerHost_KeepsOnlyTheLowercaseHostOfWebReferrers(string? referrer, string? expected)
    {
        Assert.Equal(expected, UrlsService.TryGetReferrerHost(referrer));
    }

    // ---------- normalización y validación de URLs ----------

    [Theory]
    [InlineData("www.google.com", "https://www.google.com/")]
    [InlineData("  www.google.com  ", "https://www.google.com/")]
    [InlineData("google.com/search?q=snap", "https://google.com/search?q=snap")]
    [InlineData("https://www.google.com", "https://www.google.com/")]
    [InlineData("http://example.com/path", "http://example.com/path")]
    [InlineData("HTTPS://Example.COM/Path", "https://example.com/Path")]
    [InlineData("//cdn.example.com/lib.js", "https://cdn.example.com/lib.js")]
    [InlineData("example.com:8443/admin", "https://example.com:8443/admin")]
    [InlineData("localhost:8080/path", "https://localhost:8080/path")]
    [InlineData("http://localhost:8080/path?q=1#frag", "http://localhost:8080/path?q=1#frag")]
    [InlineData("192.168.1.10/panel", "https://192.168.1.10/panel")]
    public void TryNormalizeUrl_ValidInput_ReturnsAbsoluteHttpUrl(string input, string expected)
    {
        Assert.True(UrlsService.TryNormalizeUrl(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("google")]
    [InlineData(".com")]
    [InlineData("/relative/path")]
    [InlineData("https://")]
    [InlineData("http:example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///etc/passwd")]
    [InlineData("mailto:a@b.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hola")]
    public void TryNormalizeUrl_InvalidInput_IsRejected(string? input)
    {
        Assert.False(UrlsService.TryNormalizeUrl(input, out var normalized));
        Assert.Null(normalized);
    }

    [Fact]
    public void TryNormalizeUrl_TooLong_IsRejected()
    {
        var url = "https://example.com/" + new string('a', UrlsService.MaxUrlLength);

        Assert.False(UrlsService.TryNormalizeUrl(url, out _));
    }

    private UrlsService CreateService(params string[] codes) =>
        new(_repository, new SequenceCodeGenerator(codes), _clock);

    private async Task<List<(string ClickedAt, string? ReferrerHost)>> ReadClicksAsync()
    {
        await using var connection = await _connections.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT clicked_at, referrer_host FROM clicks ORDER BY id;";
        var clicks = new List<(string, string?)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            clicks.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }
        return clicks;
    }

    private async Task<(long Total, string? LastClickAt)> ReadCountersAsync(string code)
    {
        await using var connection = await _connections.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT total_clicks, last_click_at FROM urls WHERE code = $code;";
        command.Parameters.AddWithValue("$code", code);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    private sealed class SequenceCodeGenerator(IEnumerable<string> codes) : IShortCodeGenerator
    {
        private readonly Queue<string> _codes = new(codes);

        public string Generate() => _codes.Dequeue();
    }
}
