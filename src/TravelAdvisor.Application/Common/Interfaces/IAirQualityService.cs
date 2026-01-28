namespace TravelAdvisor.Application.Common.Interfaces;

public interface IAirQualityService
{
    Task<AirQualityData?> GetAirQualityAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<Dictionary<string, AirQualityData>> GetAirQualityForDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default);
}
