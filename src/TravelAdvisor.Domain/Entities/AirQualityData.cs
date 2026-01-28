namespace TravelAdvisor.Domain.Entities;

public sealed class AirQualityData
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required IReadOnlyList<string> Times { get; init; }
    public required IReadOnlyList<double?> Pm25Values { get; init; }
}
