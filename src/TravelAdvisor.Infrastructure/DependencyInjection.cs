using Hangfire.Redis.StackExchange;
using TravelAdvisor.Infrastructure.Caching;
using TravelAdvisor.Infrastructure.ExternalApis;

namespace TravelAdvisor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        services.AddAutoMapper(assembly);

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHttpClient<IDistrictService, DistrictService>();
        services.AddHttpClient<IWeatherService, WeatherService>();
        services.AddHttpClient<IAirQualityService, AirQualityService>();

        services.AddScoped<CacheWarmingJob>();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redisConnectionString, new RedisStorageOptions
                {
                    Prefix = "hangfire:traveladvisor:",
                    Db = 0
                }));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1;
                options.Queues = new[] { "default", "cache-warming" };
            });
        }

        return services;
    }
}
