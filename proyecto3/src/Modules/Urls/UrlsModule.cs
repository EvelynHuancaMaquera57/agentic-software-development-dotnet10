namespace Snap.Modules.Urls;

public static class UrlsModule
{
    public static IServiceCollection AddUrlsModule(this IServiceCollection services)
    {
        services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
        services.AddSingleton<UrlsRepository>();
        services.AddSingleton<UrlsService>();
        return services;
    }
}
