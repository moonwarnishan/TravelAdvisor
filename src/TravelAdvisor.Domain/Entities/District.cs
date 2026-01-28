namespace TravelAdvisor.Domain.Entities;

public sealed class District
{
    public required string Id { get; init; }
    public required string DivisionId { get; init; }
    public required string Name { get; init; }
    public required string BnName { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}
