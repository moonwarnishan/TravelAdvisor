namespace TravelAdvisor.Application.DTOs.Districts;

public sealed record RankedDistrictDto
{
    public int Rank { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public double AverageTemperatureAt2pm { get; init; }
    public double AveragePm25 { get; init; }
}
