using System.ComponentModel;

namespace TravelAdvisor.Application.DTOs.Requests;

/// <summary>
/// Request for getting top districts for travel based on weather and air quality.
/// </summary>
public sealed class GetTopDistrictsRequest
{
    /// <summary>
    /// Number of top districts to return (1-64).
    /// </summary>
    /// <example>10</example>
    [DefaultValue(10)]
    public int Count { get; init; } = Constants.Defaults.TopDistrictsCount;
}
