using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TravelAdvisor.Application.DTOs.Requests;

/// <summary>
/// Request for getting travel recommendation between current location and destination.
/// </summary>
public sealed class TravelRecommendationRequest
{
    /// <summary>
    /// Latitude of the traveler's current location.
    /// </summary>
    /// <example>23.8103</example>
    [DefaultValue(23.8103)]
    public double CurrentLatitude { get; init; }

    /// <summary>
    /// Longitude of the traveler's current location.
    /// </summary>
    /// <example>90.4125</example>
    [DefaultValue(90.4125)]
    public double CurrentLongitude { get; init; }

    /// <summary>
    /// Name of the destination district in Bangladesh.
    /// </summary>
    /// <example>Cox's Bazar</example>
    [DefaultValue("Cox's Bazar")]
    public required string DestinationDistrict { get; init; }

    /// <summary>
    /// The planned travel date.
    /// </summary>
    /// <example>2026-02-15</example>
    public DateOnly TravelDate { get; init; }
}
