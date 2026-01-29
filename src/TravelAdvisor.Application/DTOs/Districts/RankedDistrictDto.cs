namespace TravelAdvisor.Application.DTOs.Districts;

/// <summary>
/// Represents a district ranked by travel conditions (weather and air quality).
/// </summary>
public sealed record RankedDistrictDto
{
    /// <summary>
    /// The rank position (1 = best travel conditions).
    /// </summary>
    /// <example>1</example>
    public int Rank { get; init; }

    /// <summary>
    /// Name of the district.
    /// </summary>
    /// <example>Cox's Bazar</example>
    public required string Name { get; init; }

    /// <summary>
    /// Latitude coordinate of the district.
    /// </summary>
    /// <example>21.4272</example>
    public required double Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate of the district.
    /// </summary>
    /// <example>92.0058</example>
    public required double Longitude { get; init; }

    /// <summary>
    /// Average temperature at 2pm in Celsius over the forecast period.
    /// </summary>
    /// <example>28.5</example>
    public double AverageTemperatureAt2pm { get; init; }

    /// <summary>
    /// Average PM2.5 air quality level in µg/m³.
    /// </summary>
    /// <example>15.2</example>
    public double AveragePm25 { get; init; }
}
