using Microsoft.AspNetCore.Http.HttpResults;
using Snap.Modules.Auth;

namespace Snap.Modules.Dashboard;

/// <summary>Capa HTTP del dashboard. Siempre autenticado y siempre limitado a las URLs del usuario.</summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", GetAsync).RequireAuthorization();
        return app;
    }

    private static async Task<Results<Ok<DashboardResponse>, ValidationProblem>> GetAsync(
        AuthenticatedUser user, DashboardService dashboard, HttpRequest http, CancellationToken cancellationToken,
        int? days = null)
    {
        var period = days ?? DashboardService.DefaultDays;
        if (!DashboardService.IsValidPeriod(period))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["days"] = [DashboardService.InvalidDaysMessage],
            });
        }

        var result = await dashboard.GetAsync(user.Id, period, cancellationToken);
        return TypedResults.Ok(DashboardResponse.From(result, http));
    }
}
