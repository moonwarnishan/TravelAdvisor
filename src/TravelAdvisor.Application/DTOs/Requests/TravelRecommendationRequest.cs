namespace TravelAdvisor.Application.DTOs.Requests;

public sealed class TravelRecommendationRequest
{
    public double CurrentLatitude { get; init; }
    public double CurrentLongitude { get; init; }
    public required string DestinationDistrict { get; init; }
    public DateOnly TravelDate { get; init; }
}
