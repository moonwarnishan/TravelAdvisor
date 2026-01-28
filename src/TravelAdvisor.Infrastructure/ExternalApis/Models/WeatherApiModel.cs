using System.Text.Json.Serialization;

namespace TravelAdvisor.Infrastructure.ExternalApis.Models;

public sealed class WeatherApiModel
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("hourly")]
    public WeatherHourlyModel? Hourly { get; init; }
}

public sealed class WeatherHourlyModel
{
    [JsonPropertyName("time")]
    public List<string> Time { get; init; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature2m { get; init; } = [];
}
