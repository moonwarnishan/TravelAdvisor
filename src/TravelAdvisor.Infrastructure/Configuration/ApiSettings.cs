namespace TravelAdvisor.Infrastructure.Configuration;

public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    public required string DistrictsUrl { get; init; }
    public required string WeatherApiBaseUrl { get; init; }
    public required string AirQualityApiBaseUrl { get; init; }
}
