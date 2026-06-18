using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Application.Settings;
using Payment.Infrastructure.Services;
using Shared.Common.Settings;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(nameof(MongoDbSettings)));

        services.Configure<JwtSettings>(
            configuration.GetSection(nameof(JwtSettings)));

        services.Configure<PayOsSettings>(
            configuration.GetSection(nameof(PayOsSettings)));

        services.AddSingleton<Persistence.MongoDbContext>();
        services.AddSingleton<Persistence.MongoDbInitializer>();

        services.AddScoped<IFeePolicyService, FeePolicyService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPayOsService, PayOsService>();

        return services;
    }
}
