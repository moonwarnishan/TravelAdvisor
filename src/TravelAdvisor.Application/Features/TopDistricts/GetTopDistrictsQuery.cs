namespace TravelAdvisor.Application.Features.TopDistricts;

public sealed record GetTopDistrictsQuery(int Count = Constants.Defaults.TopDistrictsCount) : IRequest<TopDistrictsResponse>;
