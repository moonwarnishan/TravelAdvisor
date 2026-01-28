using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Infrastructure.Caching;
using TravelAdvisor.Infrastructure.Configuration;
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

        return services;
    }
}
