using Hangfire.Redis.StackExchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using TravelAdvisor.Infrastructure.Caching;
using TravelAdvisor.Infrastructure.ExternalApis;
using TravelAdvisor.Infrastructure.Persistence;

namespace TravelAdvisor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        services.AddAutoMapper(assembly);

        var postgresConnectionString = configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddDbContext<TravelAdvisorDbContext>(options =>
                options.UseNpgsql(postgresConnectionString));
        }

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHttpClient<IDistrictService, DistrictService>()
            .AddStandardResilienceHandler(ConfigureResilienceOptions);

        services.AddHttpClient<IWeatherService, WeatherService>()
            .AddStandardResilienceHandler(ConfigureResilienceOptions);

        services.AddHttpClient<IAirQualityService, AirQualityService>()
            .AddStandardResilienceHandler(ConfigureResilienceOptions);

        services.AddScoped<CacheWarmingJob>();
        services.AddScoped<DistrictSyncJob>();

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

    private static void ConfigureResilienceOptions(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    }
}
