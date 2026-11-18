namespace Snap.Modules.Dashboard;

public static class DashboardModule
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        services.AddSingleton<DashboardRepository>();
        services.AddSingleton<DashboardService>();
        return services;
    }
}
