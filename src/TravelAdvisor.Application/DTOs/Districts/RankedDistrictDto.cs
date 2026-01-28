namespace TravelAdvisor.Application.DTOs.Districts;

public sealed class RankedDistrictDto
{
    public required int Rank { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required double AverageTemperatureAt2pm { get; init; }
    public required double AveragePm25 { get; init; }
}
