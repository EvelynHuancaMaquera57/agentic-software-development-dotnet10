namespace Snap.Data;

public static class DataModule
{
    public const string ConnectionStringName = "Snap";

    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Falta la cadena de conexión '{ConnectionStringName}'.");
            return new SqliteConnectionFactory(connectionString);
        });
        return services;
    }

    public static Task InitializeDatabaseAsync(this WebApplication app) =>
        DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<SqliteConnectionFactory>());
}
