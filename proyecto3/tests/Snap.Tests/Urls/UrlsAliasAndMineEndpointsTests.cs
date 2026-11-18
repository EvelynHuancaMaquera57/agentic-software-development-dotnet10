using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Snap.Modules.Auth;
using Snap.Modules.Urls;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Urls;

/// <summary>Alias personalizado en POST /api/urls y listado privado GET /api/urls/mine.</summary>
public sealed class UrlsAliasAndMineEndpointsTests : IAsyncLifetime
{
    private readonly SnapApiFactory _factory = new();
    private HttpClient _client = null!;
    private AuthResponse _ana = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _ana = await _client.RegisterUserAsync("ana@example.com", "Ana");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- alias ----------

    [Fact]
    public async Task Post_WithAlias_UsesItAsCodeAndRedirects()
    {
        using var response = await PostAsync("www.google.com", "mi-google_1", _ana.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<UrlResponse>())!;
        Assert.Equal("mi-google_1", body.Code);
        Assert.Equal("http://localhost/mi-google_1", body.ShortUrl);

        using var redirect = await _client.GetAsync("/mi-google_1");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal(new Uri("https://www.google.com/"), redirect.Headers.Location);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_WithoutAlias_GeneratesRandomCode(string? alias)
    {
        using var response = await PostAsync("https://example.com", alias, _ana.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches("^[A-Za-z0-9]{7}$", (await response.Content.ReadFromJsonAsync<UrlResponse>())!.Code);
    }

    [Fact]
    public async Task Post_AliasWithSurroundingSpaces_IsTrimmed()
    {
        using var response = await PostAsync("https://example.com", "  promo  ", _ana.Token);

        Assert.Equal("promo", (await response.Content.ReadFromJsonAsync<UrlResponse>())!.Code);
    }

    [Theory]
    [InlineData("ab", UrlsService.InvalidAliasMessage)]
    [InlineData("con espacio", UrlsService.InvalidAliasMessage)]
    [InlineData("eñe", UrlsService.InvalidAliasMessage)]
    [InlineData("a/b", UrlsService.InvalidAliasMessage)]
    [InlineData("1234567890123456789012345678901", UrlsService.InvalidAliasMessage)]
    [InlineData("health", UrlsService.ReservedAliasMessage)]
    [InlineData("API", UrlsService.ReservedAliasMessage)]
    [InlineData("Login", UrlsService.ReservedAliasMessage)]
    public async Task Post_InvalidOrReservedAlias_Returns400WithAliasError(string alias, string expected)
    {
        using var response = await PostAsync("https://example.com", alias, _ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Equal([expected], problem.Errors["alias"]);
    }

    [Fact]
    public async Task Post_InvalidUrlAndAlias_ReturnsBothErrorsAtOnce()
    {
        using var response = await PostAsync("google", "x", _ana.Token);

        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Equal(["alias", "url"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task Post_AliasAlreadyUsedByAnotherUser_Returns409AndKeepsOriginal()
    {
        var beto = await _client.RegisterUserAsync("beto@example.com", "Beto");
        (await PostAsync("https://example.com/de-ana", "promo", _ana.Token)).Dispose();

        using var response = await PostAsync("https://example.com/de-beto", "promo", beto.Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!;
        Assert.Equal(UrlsService.AliasTakenMessage, problem.Detail);
        using var redirect = await _client.GetAsync("/promo");
        Assert.Equal(new Uri("https://example.com/de-ana"), redirect.Headers.Location);
    }

    // ---------- GET /api/urls/mine ----------

    [Fact]
    public async Task Mine_WithoutToken_Returns401()
    {
        using var response = await _client.GetAsync("/api/urls/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mine_NoUrls_ReturnsEmptyPage()
    {
        var page = await GetMineAsync(_ana.Token);

        Assert.Empty(page.Items);
        Assert.Equal((1, UrlsService.DefaultPageSize, 0L), (page.Page, page.PageSize, page.Total));
    }

    [Fact]
    public async Task Mine_ReturnsOnlyOwnUrls_NewestFirst_WithClickStats()
    {
        var beto = await _client.RegisterUserAsync("beto@example.com", "Beto");
        (await PostAsync("https://example.com/1", "ana-uno", _ana.Token)).Dispose();
        (await PostAsync("https://example.com/beto", "beto-uno", beto.Token)).Dispose();
        (await PostAsync("https://example.com/2", "ana-dos", _ana.Token)).Dispose();
        (await _client.GetAsync("/ana-uno")).Dispose();
        (await _client.GetAsync("/ana-uno")).Dispose();

        var page = await GetMineAsync(_ana.Token);

        Assert.Equal(2, page.Total);
        Assert.Equal(["ana-dos", "ana-uno"], page.Items.Select(url => url.Code));
        var visited = page.Items[1];
        Assert.Equal(2, visited.TotalClicks);
        Assert.NotNull(visited.LastClickAt);
        Assert.Equal("http://localhost/ana-uno", visited.ShortUrl);
        Assert.Equal("https://example.com/1", visited.OriginalUrl);
        Assert.Equal(0, page.Items[0].TotalClicks);
        Assert.Null(page.Items[0].LastClickAt);
    }

    [Fact]
    public async Task Mine_Paginates()
    {
        for (var i = 1; i <= 5; i++)
        {
            (await PostAsync($"https://example.com/{i}", $"url-{i}", _ana.Token)).Dispose();
        }

        var first = await GetMineAsync(_ana.Token, "?page=1&pageSize=2");
        var last = await GetMineAsync(_ana.Token, "?page=3&pageSize=2");
        var beyond = await GetMineAsync(_ana.Token, "?page=4&pageSize=2");

        Assert.Equal(["url-5", "url-4"], first.Items.Select(url => url.Code));
        Assert.Equal(["url-1"], last.Items.Select(url => url.Code));
        Assert.Empty(beyond.Items);
        Assert.All([first, last, beyond], page => Assert.Equal(5, page.Total));
    }

    [Theory]
    [InlineData("?page=0", "page")]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=101", "pageSize")]
    public async Task Mine_InvalidPaging_Returns400WithField(string query, string field)
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, $"/api/urls/mine{query}", _ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.True(problem.Errors.ContainsKey(field));
    }

    // ---------- helpers ----------

    private Task<HttpResponseMessage> PostAsync(string url, string? alias, string token) =>
        _client.SendAuthenticatedAsync(HttpMethod.Post, "/api/urls", token, new CreateUrlRequest(url, alias));

    private async Task<PagedResponse<MyUrlResponse>> GetMineAsync(string token, string query = "")
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, $"/api/urls/mine{query}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<MyUrlResponse>>())!;
    }
}
