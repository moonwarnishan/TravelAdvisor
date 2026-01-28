using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.ExternalApis.Models;

namespace TravelAdvisor.Infrastructure.ExternalApis;

public sealed class WeatherService(
    HttpClient httpClient,
    ICacheService cacheService,
    IOptions<ApiSettings> apiSettings,
    ILogger<WeatherService> logger) : IWeatherService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private readonly ApiSettings _apiSettings = apiSettings.Value;

    public async Task<WeatherData?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{Constants.CacheKeys.WeatherPrefix}:{latitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}:{longitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}";

        var cached = await cacheService.GetAsync<WeatherData>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var url = $"{_apiSettings.WeatherApiBaseUrl}?latitude={latitude}&longitude={longitude}&hourly={Constants.ApiParameters.HourlyTemperature}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";

        try
        {
            var response = await httpClient.GetStringAsync(url, cancellationToken);
            var apiModel = JsonSerializer.Deserialize<WeatherApiModel>(response);

            if (apiModel?.Hourly is null)
                return null;

            var weatherData = new WeatherData
            {
                Latitude = apiModel.Latitude,
                Longitude = apiModel.Longitude,
                Times = apiModel.Hourly.Time,
                Temperatures = apiModel.Hourly.Temperature2m
            };

            await cacheService.SetAsync(cacheKey, weatherData, CacheExpiration, cancellationToken);
            return weatherData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch weather data for coordinates: {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    public async Task<Dictionary<string, WeatherData>> GetWeatherForDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, WeatherData>();
        var tasks = districts.Select(async district =>
        {
            var weather = await GetWeatherAsync(district.Latitude, district.Longitude, cancellationToken);
            return (district.Name, Weather: weather);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (name, weather) in completedTasks)
        {
            if (weather is not null)
            {
                results[name] = weather;
            }
        }

        return results;
    }
}
