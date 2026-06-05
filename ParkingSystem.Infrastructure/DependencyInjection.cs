using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkingSystem.Infrastructure.Persistence;
using ParkingSystem.Infrastructure.Settings;

namespace ParkingSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(nameof(MongoDbSettings)));

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoDbInitializer>();

        return services;
    }
}
