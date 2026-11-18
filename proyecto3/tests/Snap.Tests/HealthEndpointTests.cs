using System.Net;
using System.Net.Http.Json;
using Snap.Tests.Infrastructure;

namespace Snap.Tests;

public sealed class HealthEndpointTests(SnapApiFactory factory) : IClassFixture<SnapApiFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsOkWithHealthyStatus()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("snap", body.Service);
        Assert.True(body.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}
