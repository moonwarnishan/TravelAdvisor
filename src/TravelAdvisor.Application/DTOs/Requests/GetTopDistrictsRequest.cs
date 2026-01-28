namespace TravelAdvisor.Application.DTOs.Requests;

public sealed class GetTopDistrictsRequest
{
    public int Count { get; init; } = Constants.Defaults.TopDistrictsCount;
}
