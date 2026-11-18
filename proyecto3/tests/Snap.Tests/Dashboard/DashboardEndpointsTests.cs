using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Snap.Modules.Auth;
using Snap.Modules.Dashboard;
using Snap.Modules.Urls;
using Snap.Tests.Infrastructure;

namespace Snap.Tests.Dashboard;

/// <summary>Pruebas de integración de GET /api/dashboard: HTTP real, SQLite real, clicks generados visitando los códigos.</summary>
public sealed class DashboardEndpointsTests : IAsyncLifetime
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

    // ---------- autenticación ----------

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        using var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("token-falso")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.firma-invalida")]
    public async Task Get_WithInvalidToken_Returns401(string token)
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, "/api/dashboard", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- usuario sin URLs ----------

    [Fact]
    public async Task Get_UserWithoutUrls_ReturnsZeros()
    {
        var dashboard = await GetDashboardAsync(_ana.Token);

        Assert.Equal(new DashboardTotals(0, 0, 0), dashboard.Totals);
        Assert.Equal(DashboardService.DefaultDays, dashboard.Period.Days);
        Assert.Equal(0, dashboard.Period.Clicks);
        Assert.Equal(0, dashboard.Period.PreviousClicks);
        Assert.Null(dashboard.Period.ChangePercent);
        Assert.Equal(DashboardService.DefaultDays, dashboard.Daily.Count);
        Assert.All(dashboard.Daily, day => Assert.Equal(0, day.Clicks));
        Assert.Empty(dashboard.Urls);
    }

    // ---------- usuario con URLs y clicks ----------

    [Fact]
    public async Task Get_UserWithUrlsAndClicks_ReturnsCorrectDataOnlyForThatUser()
    {
        var popular = await CreateUrlAsync("https://example.com/popular", _ana.Token);
        var unvisited = await CreateUrlAsync("https://example.com/sin-visitas", _ana.Token);
        await VisitAsync(popular.Code, referrer: "https://twitter.com/ana/status/1");
        await VisitAsync(popular.Code);
        await VisitAsync(popular.Code);

        var beto = await _client.RegisterUserAsync("beto@example.com", "Beto");
        var betoUrl = await CreateUrlAsync("https://example.com/de-beto", beto.Token);
        await VisitAsync(betoUrl.Code);

        var dashboard = await GetDashboardAsync(_ana.Token);

        Assert.Equal(new DashboardTotals(Urls: 2, Clicks: 3, UrlsWithoutClicks: 1), dashboard.Totals);
        Assert.Equal(3, dashboard.Period.Clicks);
        Assert.Equal(0, dashboard.Period.PreviousClicks);
        Assert.Null(dashboard.Period.ChangePercent);
        Assert.Equal(3, dashboard.Daily.Sum(day => day.Clicks));
        Assert.Equal(DateOnly.FromDateTime(dashboard.Period.To.UtcDateTime), dashboard.Daily[^1].Date);

        Assert.Equal([popular.Code, unvisited.Code], dashboard.Urls.Select(url => url.Code));
        var top = dashboard.Urls[0];
        Assert.Equal("https://example.com/popular", top.OriginalUrl);
        Assert.Equal($"http://localhost/{popular.Code}", top.ShortUrl);
        Assert.Equal((3L, 3L, 0L), (top.TotalClicks, top.PeriodClicks, top.PreviousPeriodClicks));
        Assert.NotNull(top.LastClickAt);
        Assert.Null(dashboard.Urls[1].LastClickAt);

        // Y Beto solo ve lo suyo.
        var betoDashboard = await GetDashboardAsync(beto.Token);
        Assert.Equal(new DashboardTotals(Urls: 1, Clicks: 1, UrlsWithoutClicks: 0), betoDashboard.Totals);
        Assert.Equal([betoUrl.Code], betoDashboard.Urls.Select(url => url.Code));
    }

    [Fact]
    public async Task Get_AfterDeletingUrl_ItsClicksNoLongerCount()
    {
        var url = await CreateUrlAsync("https://example.com/temporal", _ana.Token);
        await VisitAsync(url.Code);
        (await _client.SendAuthenticatedAsync(HttpMethod.Delete, $"/api/urls/{url.Code}", _ana.Token)).Dispose();

        var dashboard = await GetDashboardAsync(_ana.Token);

        Assert.Equal(new DashboardTotals(0, 0, 0), dashboard.Totals);
        Assert.Equal(0, dashboard.Period.Clicks);
    }

    // ---------- parámetro days ----------

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task Get_WithValidDays_ReturnsThatManyDays(int days)
    {
        var dashboard = await GetDashboardAsync(_ana.Token, $"?days={days}");

        Assert.Equal(days, dashboard.Period.Days);
        Assert.Equal(days, dashboard.Daily.Count);
        Assert.Equal(TimeSpan.FromDays(days), dashboard.Period.To - dashboard.Period.From);
    }

    [Theory]
    [InlineData("15")]
    [InlineData("0")]
    [InlineData("-7")]
    public async Task Get_WithUnsupportedDays_Returns400WithDaysError(string days)
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, $"/api/dashboard?days={days}", _ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([DashboardService.InvalidDaysMessage], problem.Errors["days"]);
    }

    [Fact]
    public async Task Get_WithNonNumericDays_Returns400()
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, "/api/dashboard?days=abc", _ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- helpers ----------

    private async Task<DashboardResponse> GetDashboardAsync(string token, string query = "")
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Get, $"/api/dashboard{query}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DashboardResponse>())!;
    }

    private async Task<UrlResponse> CreateUrlAsync(string url, string token)
    {
        using var response = await _client.SendAuthenticatedAsync(HttpMethod.Post, "/api/urls", token, new CreateUrlRequest(url));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UrlResponse>())!;
    }

    private async Task VisitAsync(string code, string? referrer = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/{code}");
        if (referrer is not null)
        {
            request.Headers.Referrer = new Uri(referrer);
        }
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
