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
        var cacheKey = BuildCacheKey(latitude, longitude);

        var cached = await cacheService.GetAsync<WeatherData>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var url = BuildSingleUrl(latitude, longitude);

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
        var districtList = districts.ToList();
        var results = new Dictionary<string, WeatherData>();
        var uncachedDistricts = new List<District>();

        foreach (var district in districtList)
        {
            var cacheKey = BuildCacheKey(district.Latitude, district.Longitude);
            var cached = await cacheService.GetAsync<WeatherData>(cacheKey, cancellationToken);

            if (cached is not null)
            {
                results[district.Name] = cached;
            }
            else
            {
                uncachedDistricts.Add(district);
            }
        }

        if (uncachedDistricts.Count == 0)
        {
            logger.LogDebug("All weather data retrieved from cache");
            return results;
        }

        logger.LogInformation("Fetching weather data for {Count} districts via batch API", uncachedDistricts.Count);

        try
        {
            var batchResults = await FetchBatchWeatherAsync(uncachedDistricts, cancellationToken);

            foreach (var (district, weatherData) in batchResults)
            {
                results[district.Name] = weatherData;

                var cacheKey = BuildCacheKey(district.Latitude, district.Longitude);
                await cacheService.SetAsync(cacheKey, weatherData, CacheExpiration, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch batch weather data");
        }

        return results;
    }

    private async Task<List<(District District, WeatherData Data)>> FetchBatchWeatherAsync(
        List<District> districts,
        CancellationToken cancellationToken)
    {
        var results = new List<(District, WeatherData)>();

        var latitudes = string.Join(",", districts.Select(d => d.Latitude.ToString("F4")));
        var longitudes = string.Join(",", districts.Select(d => d.Longitude.ToString("F4")));

        var url = $"{_apiSettings.WeatherApiBaseUrl}?latitude={latitudes}&longitude={longitudes}&hourly={Constants.ApiParameters.HourlyTemperature}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";

        var response = await httpClient.GetStringAsync(url, cancellationToken);

        if (districts.Count == 1)
        {
            var singleModel = JsonSerializer.Deserialize<WeatherApiModel>(response);
            if (singleModel?.Hourly is not null)
            {
                var weatherData = new WeatherData
                {
                    Latitude = singleModel.Latitude,
                    Longitude = singleModel.Longitude,
                    Times = singleModel.Hourly.Time,
                    Temperatures = singleModel.Hourly.Temperature2m
                };
                results.Add((districts[0], weatherData));
            }
        }
        else
        {
            var batchModels = JsonSerializer.Deserialize<List<WeatherApiModel>>(response);
            if (batchModels is not null)
            {
                for (var i = 0; i < batchModels.Count && i < districts.Count; i++)
                {
                    var model = batchModels[i];
                    if (model.Hourly is not null)
                    {
                        var weatherData = new WeatherData
                        {
                            Latitude = model.Latitude,
                            Longitude = model.Longitude,
                            Times = model.Hourly.Time,
                            Temperatures = model.Hourly.Temperature2m
                        };
                        results.Add((districts[i], weatherData));
                    }
                }
            }
        }

        return results;
    }

    private static string BuildCacheKey(double latitude, double longitude)
    {
        return $"{Constants.CacheKeys.WeatherPrefix}:{latitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}:{longitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}";
    }

    private string BuildSingleUrl(double latitude, double longitude)
    {
        return $"{_apiSettings.WeatherApiBaseUrl}?latitude={latitude}&longitude={longitude}&hourly={Constants.ApiParameters.HourlyTemperature}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";
    }
}
