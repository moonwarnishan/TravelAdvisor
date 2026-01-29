namespace TravelAdvisor.Application.DTOs.Districts;

/// <summary>
/// Weather and air quality data for a specific location.
/// </summary>
public sealed record LocationComparisonDto
{
    /// <summary>
    /// Name of the location or district.
    /// </summary>
    /// <example>Dhaka</example>
    public required string Name { get; init; }

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    /// <example>23.8103</example>
    public required double Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    /// <example>90.4125</example>
    public required double Longitude { get; init; }

    /// <summary>
    /// Temperature at 2pm in Celsius for the travel date.
    /// </summary>
    /// <example>32.5</example>
    public double TemperatureAt2pm { get; init; }

    /// <summary>
    /// PM2.5 air quality level in µg/m³.
    /// </summary>
    /// <example>45.8</example>
    public double Pm25Level { get; init; }
}
