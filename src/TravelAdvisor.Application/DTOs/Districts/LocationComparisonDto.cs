namespace TravelAdvisor.Application.DTOs.Districts;

public sealed record LocationComparisonDto
{
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public double TemperatureAt2pm { get; init; }
    public double Pm25Level { get; init; }
}
