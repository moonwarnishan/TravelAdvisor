namespace TravelAdvisor.Application.Features.TravelRecommendation;

public sealed record GetTravelRecommendationQuery(
    double CurrentLatitude,
    double CurrentLongitude,
    string DestinationDistrict,
    DateOnly TravelDate) : IRequest<TravelRecommendationResponse>;
