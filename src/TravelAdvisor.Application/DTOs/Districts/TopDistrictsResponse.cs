namespace TravelAdvisor.Application.DTOs.Districts;

public sealed class TopDistrictsResponse
{
    public required IReadOnlyList<RankedDistrictDto> Districts { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required string ForecastPeriod { get; init; }
}
