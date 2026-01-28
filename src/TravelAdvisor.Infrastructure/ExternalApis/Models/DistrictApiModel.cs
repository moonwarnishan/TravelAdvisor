namespace TravelAdvisor.Infrastructure.ExternalApis.Models;

public sealed class DistrictListApiModel
{
    [JsonPropertyName("districts")]
    public List<DistrictApiModel> Districts { get; init; } = [];
}

public sealed class DistrictApiModel
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("division_id")]
    public required string DivisionId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("bn_name")]
    public required string BnName { get; init; }

    [JsonPropertyName("lat")]
    public required string Lat { get; init; }

    [JsonPropertyName("long")]
    public required string Long { get; init; }
}
