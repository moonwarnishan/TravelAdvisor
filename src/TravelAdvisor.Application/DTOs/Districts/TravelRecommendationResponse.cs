namespace TravelAdvisor.Application.DTOs.Districts;

/// <summary>
/// Response containing a travel recommendation comparing current location with destination.
/// </summary>
public sealed class TravelRecommendationResponse
{
    /// <summary>
    /// The travel recommendation (e.g., "Recommended", "Not Recommended").
    /// </summary>
    /// <example>Recommended</example>
    public required string Recommendation { get; init; }

    /// <summary>
    /// Explanation for the recommendation based on weather and air quality comparison.
    /// </summary>
    /// <example>The destination has better weather conditions with lower temperature (28.5°C vs 32.5°C) and significantly better air quality (PM2.5: 15.2 vs 45.8 µg/m³).</example>
    public required string Reason { get; init; }

    /// <summary>
    /// Weather and air quality data for the current location.
    /// </summary>
    public required LocationComparisonDto CurrentLocation { get; init; }

    /// <summary>
    /// Weather and air quality data for the destination district.
    /// </summary>
    public required LocationComparisonDto Destination { get; init; }
}
