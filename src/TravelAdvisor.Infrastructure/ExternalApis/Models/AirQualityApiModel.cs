namespace TravelAdvisor.Infrastructure.ExternalApis.Models;

public sealed class AirQualityApiModel
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("hourly")]
    public AirQualityHourlyModel? Hourly { get; init; }
}

public sealed class AirQualityHourlyModel
{
    [JsonPropertyName("time")]
    public List<string> Time { get; init; } = [];

    [JsonPropertyName("pm2_5")]
    public List<double?> Pm25 { get; init; } = [];
}
