namespace TravelAdvisor.Infrastructure.Configuration;

public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public string? RedisConnectionString { get; init; }
    public int DefaultExpirationMinutes { get; init; } = 15;
    public int DistrictsCacheHours { get; init; } = 24;
    public int WeatherCacheMinutes { get; init; } = 30;
    public int AirQualityCacheMinutes { get; init; } = 30;
}
