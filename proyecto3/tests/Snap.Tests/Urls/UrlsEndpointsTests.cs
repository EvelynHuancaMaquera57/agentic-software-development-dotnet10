using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Snap.Data;
using Snap.Modules.Auth;
using Snap.Modules.Urls;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Urls;

/// <summary>
/// Pruebas de integración: HTTP → servicio → SQLite real. Cada test tiene su propia base.
/// Crear y borrar van autenticados; listar y redirigir se prueban sin token a propósito.
/// </summary>
public sealed class UrlsEndpointsTests : IAsyncLifetime
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

    // ---------- POST /api/urls (protegido) ----------

    [Fact]
    public async Task Post_ValidUrl_Returns201WithShortCode()
    {
        using var response = await PostUrlAsync("https://example.com/a/very/long/path?x=1", _ana.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(body);
        Assert.Equal("https://example.com/a/very/long/path?x=1", body.OriginalUrl);
        Assert.Matches("^[A-Za-z0-9]{7}$", body.Code);
        Assert.Equal($"http://localhost/{body.Code}", body.ShortUrl);
        Assert.Equal(new Uri(body.ShortUrl), response.Headers.Location);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401AndCreatesNothing()
    {
        using var response = await _client.PostAsJsonAsync("/api/urls", new CreateUrlRequest("https://example.com"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<UrlResponse[]>("/api/urls"))!);
    }

    [Fact]
    public async Task Post_WithInvalidToken_Returns401()
    {
        using var response = await PostUrlAsync("https://example.com", "token-falso");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_UrlWithoutScheme_AddsHttpsAndRedirectsCorrectly()
    {
        using var response = await PostUrlAsync("www.google.com", _ana.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(body);
        Assert.Equal("https://www.google.com/", body.OriginalUrl);

        using var redirect = await _client.GetAsync($"/{body.Code}");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal(new Uri("https://www.google.com/"), redirect.Headers.Location);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("google")]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com/file")]
    [InlineData("mailto:a@b.com")]
    [InlineData("javascript:alert(1)")]
    public async Task Post_InvalidUrl_Returns400WithUrlError(string url)
    {
        using var response = await PostUrlAsync(url, _ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([UrlsService.InvalidUrlMessage], problem.Errors["url"]);
    }

    [Fact]
    public async Task Post_MissingUrl_Returns400()
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Post, "/api/urls", _ana.Token, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /{code} (público) ----------

    [Fact]
    public async Task Get_ShortCode_RedirectsWithoutToken()
    {
        var created = await CreateAsync("https://example.com/destino", _ana.Token);

        using var response = await _client.GetAsync($"/{created.Code}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(new Uri("https://example.com/destino"), response.Headers.Location);
    }

    [Fact]
    public async Task Get_UnknownCode_Returns404AndRecordsNoClick()
    {
        using var response = await _client.GetAsync("/noexiste");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountClicksAsync());
    }

    [Fact]
    public async Task Get_ShortCode_RecordsOneClickPerVisitWithReferrerHost()
    {
        var created = await CreateAsync("https://example.com/destino", _ana.Token);

        using var fromTwitter = new HttpRequestMessage(HttpMethod.Get, $"/{created.Code}");
        fromTwitter.Headers.Referrer = new Uri("https://twitter.com/ana/status/1");
        (await _client.SendAsync(fromTwitter)).Dispose();
        (await _client.GetAsync($"/{created.Code}")).Dispose();

        Assert.Equal(2, await CountClicksAsync());
        Assert.Equal(1, await CountClicksAsync("referrer_host = 'twitter.com'"));
        Assert.Equal(1, await CountClicksAsync("referrer_host IS NULL"));
    }

    [Fact]
    public async Task Get_Health_IsNotCapturedByShortCodeRoute()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- GET /api/urls (público) ----------

    [Fact]
    public async Task List_Empty_ReturnsEmptyArray()
    {
        var urls = await _client.GetFromJsonAsync<UrlResponse[]>("/api/urls");

        Assert.NotNull(urls);
        Assert.Empty(urls);
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUrlsOfAllUsers_NewestFirst()
    {
        var beto = await _client.RegisterUserAsync("beto@example.com", "Beto");
        var first = await CreateAsync("https://example.com/1", _ana.Token);
        var second = await CreateAsync("https://example.com/2", beto.Token);
        var third = await CreateAsync("https://example.com/3", _ana.Token);

        var urls = await _client.GetFromJsonAsync<UrlResponse[]>("/api/urls");

        Assert.NotNull(urls);
        Assert.Equal([third.Code, second.Code, first.Code], urls.Select(u => u.Code));
        Assert.Equal("https://example.com/3", urls[0].OriginalUrl);
    }

    // ---------- DELETE /api/urls/{code} (protegido, solo el dueño) ----------

    [Fact]
    public async Task Delete_OwnUrl_Returns204AndRemovesIt()
    {
        var created = await CreateAsync("https://example.com/borrar", _ana.Token);

        using var response = await DeleteAsync(created.Code, _ana.Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var redirect = await _client.GetAsync($"/{created.Code}");
        Assert.Equal(HttpStatusCode.NotFound, redirect.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<UrlResponse[]>("/api/urls"))!);
    }

    [Fact]
    public async Task Delete_OwnUrl_AlsoDeletesItsClicks()
    {
        var created = await CreateAsync("https://example.com/borrar", _ana.Token);
        (await _client.GetAsync($"/{created.Code}")).Dispose();
        (await _client.GetAsync($"/{created.Code}")).Dispose();
        Assert.Equal(2, await CountClicksAsync());

        using var response = await DeleteAsync(created.Code, _ana.Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await CountClicksAsync());
    }

    [Fact]
    public async Task Delete_UrlOfAnotherUser_Returns403AndKeepsIt()
    {
        var beto = await _client.RegisterUserAsync("beto@example.com", "Beto");
        var created = await CreateAsync("https://example.com/de-ana", _ana.Token);

        using var response = await DeleteAsync(created.Code, beto.Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var redirect = await _client.GetAsync($"/{created.Code}");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownCode_Returns404()
    {
        using var response = await DeleteAsync("noexiste", _ana.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401AndKeepsUrl()
    {
        var created = await CreateAsync("https://example.com/de-ana", _ana.Token);

        using var response = await _client.DeleteAsync($"/api/urls/{created.Code}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var redirect = await _client.GetAsync($"/{created.Code}");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
    }

    [Fact]
    public async Task Delete_SameUrlTwice_SecondReturns404()
    {
        var created = await CreateAsync("https://example.com/borrar", _ana.Token);
        (await DeleteAsync(created.Code, _ana.Token)).Dispose();

        using var response = await DeleteAsync(created.Code, _ana.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- helpers ----------

    private Task<HttpResponseMessage> PostUrlAsync(string url, string token) =>
        _client.SendAuthenticatedAsync(HttpMethod.Post, "/api/urls", token, new CreateUrlRequest(url));

    private Task<HttpResponseMessage> DeleteAsync(string code, string token) =>
        _client.SendAuthenticatedAsync(HttpMethod.Delete, $"/api/urls/{code}", token);

    private async Task<long> CountClicksAsync(string where = "1 = 1")
    {
        await using var connection = await _factory.Services.GetRequiredService<SqliteConnectionFactory>().OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM clicks WHERE {where};";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<UrlResponse> CreateAsync(string url, string token)
    {
        using var response = await PostUrlAsync(url, token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UrlResponse>())!;
    }
}
