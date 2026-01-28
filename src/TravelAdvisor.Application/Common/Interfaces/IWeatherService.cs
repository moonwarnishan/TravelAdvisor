using TravelAdvisor.Domain.Entities;

namespace TravelAdvisor.Application.Common.Interfaces;

public interface IWeatherService
{
    Task<WeatherData?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<Dictionary<string, WeatherData>> GetWeatherForDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default);
}
