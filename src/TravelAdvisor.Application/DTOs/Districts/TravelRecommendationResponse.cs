namespace TravelAdvisor.Application.DTOs.Districts;

public sealed class TravelRecommendationResponse
{
    public required string Recommendation { get; init; }
    public required string Reason { get; init; }
    public required LocationComparisonDto CurrentLocation { get; init; }
    public required LocationComparisonDto Destination { get; init; }
}
