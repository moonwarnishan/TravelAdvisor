namespace TravelAdvisor.Application.DTOs.Districts;

/// <summary>
/// Response containing the top-ranked districts for travel.
/// </summary>
public sealed class TopDistrictsResponse
{
    /// <summary>
    /// List of districts ranked by travel conditions (best first).
    /// </summary>
    public required IReadOnlyList<RankedDistrictDto> Districts { get; init; }

    /// <summary>
    /// Timestamp when this data was generated.
    /// </summary>
    /// <example>2026-01-30T14:30:00Z</example>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Description of the forecast period used for ranking.
    /// </summary>
    /// <example>Next 7 days</example>
    public required string ForecastPeriod { get; init; }
}
