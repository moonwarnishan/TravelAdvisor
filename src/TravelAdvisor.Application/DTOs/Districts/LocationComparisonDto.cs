namespace TravelAdvisor.Application.DTOs.Districts;

public sealed class LocationComparisonDto
{
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required double TemperatureAt2pm { get; init; }
    public required double Pm25Level { get; init; }
}
