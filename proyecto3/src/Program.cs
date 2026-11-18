using Snap;
using Snap.Data;
using Snap.Modules.Auth;
using Snap.Modules.Dashboard;
using Snap.Modules.Urls;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDatabase();
builder.Services.AddUrlsModule();
builder.Services.AddAuthModule();
builder.Services.AddDashboardModule();

var app = builder.Build();
await app.InitializeDatabaseAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", (TimeProvider clock) => Results.Ok(new HealthResponse(
    Status: "ok",
    Service: "snap",
    Timestamp: clock.GetUtcNow())));

app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapUrlsEndpoints();

await app.RunAsync();

// Expone Program para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
