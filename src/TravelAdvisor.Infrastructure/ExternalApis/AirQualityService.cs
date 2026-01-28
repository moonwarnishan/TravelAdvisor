namespace TravelAdvisor.Infrastructure.ExternalApis;

public sealed class AirQualityService(
    HttpClient httpClient,
    ICacheService cacheService,
    IOptions<ApiSettings> apiSettings,
    ILogger<AirQualityService> logger) : IAirQualityService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private readonly ApiSettings _apiSettings = apiSettings.Value;

    public async Task<AirQualityData?> GetAirQualityAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{Constants.CacheKeys.AirQualityPrefix}:{latitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}:{longitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}";

        var cached = await cacheService.GetAsync<AirQualityData>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var url = $"{_apiSettings.AirQualityApiBaseUrl}?latitude={latitude}&longitude={longitude}&hourly={Constants.ApiParameters.HourlyPm25}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";

        try
        {
            var response = await httpClient.GetStringAsync(url, cancellationToken);
            var apiModel = JsonSerializer.Deserialize<AirQualityApiModel>(response);

            if (apiModel?.Hourly is null)
                return null;

            var airQualityData = new AirQualityData
            {
                Latitude = apiModel.Latitude,
                Longitude = apiModel.Longitude,
                Times = apiModel.Hourly.Time,
                Pm25Values = apiModel.Hourly.Pm25
            };

            await cacheService.SetAsync(cacheKey, airQualityData, CacheExpiration, cancellationToken);
            return airQualityData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch air quality data for coordinates: {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    public async Task<Dictionary<string, AirQualityData>> GetAirQualityForDistrictsAsync(IEnumerable<District> districts, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, AirQualityData>();
        var tasks = districts.Select(async district =>
        {
            var airQuality = await GetAirQualityAsync(district.Latitude, district.Longitude, cancellationToken);
            return (district.Name, AirQuality: airQuality);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (name, airQuality) in completedTasks)
        {
            if (airQuality is not null)
            {
                results[name] = airQuality;
            }
        }

        return results;
    }
}
