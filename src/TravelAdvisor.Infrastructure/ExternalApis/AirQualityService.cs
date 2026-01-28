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
        var cacheKey = BuildCacheKey(latitude, longitude);

        var cached = await cacheService.GetAsync<AirQualityData>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var url = BuildSingleUrl(latitude, longitude);

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
        var districtList = districts.ToList();
        var results = new Dictionary<string, AirQualityData>();
        var uncachedDistricts = new List<District>();

        foreach (var district in districtList)
        {
            var cacheKey = BuildCacheKey(district.Latitude, district.Longitude);
            var cached = await cacheService.GetAsync<AirQualityData>(cacheKey, cancellationToken);

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
            logger.LogDebug("All air quality data retrieved from cache");
            return results;
        }

        logger.LogInformation("Fetching air quality data for {Count} districts via batch API", uncachedDistricts.Count);

        try
        {
            var batchResults = await FetchBatchAirQualityAsync(uncachedDistricts, cancellationToken);

            foreach (var (district, airQualityData) in batchResults)
            {
                results[district.Name] = airQualityData;

                var cacheKey = BuildCacheKey(district.Latitude, district.Longitude);
                await cacheService.SetAsync(cacheKey, airQualityData, CacheExpiration, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch batch air quality data");
        }

        return results;
    }

    private async Task<List<(District District, AirQualityData Data)>> FetchBatchAirQualityAsync(
        List<District> districts,
        CancellationToken cancellationToken)
    {
        var results = new List<(District, AirQualityData)>();

        var latitudes = string.Join(",", districts.Select(d => d.Latitude.ToString("F4")));
        var longitudes = string.Join(",", districts.Select(d => d.Longitude.ToString("F4")));

        var url = $"{_apiSettings.AirQualityApiBaseUrl}?latitude={latitudes}&longitude={longitudes}&hourly={Constants.ApiParameters.HourlyPm25}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";

        var response = await httpClient.GetStringAsync(url, cancellationToken);

        if (districts.Count == 1)
        {
            var singleModel = JsonSerializer.Deserialize<AirQualityApiModel>(response);
            if (singleModel?.Hourly is not null)
            {
                var airQualityData = new AirQualityData
                {
                    Latitude = singleModel.Latitude,
                    Longitude = singleModel.Longitude,
                    Times = singleModel.Hourly.Time,
                    Pm25Values = singleModel.Hourly.Pm25
                };
                results.Add((districts[0], airQualityData));
            }
        }
        else
        {
            var batchModels = JsonSerializer.Deserialize<List<AirQualityApiModel>>(response);
            if (batchModels is not null)
            {
                for (var i = 0; i < batchModels.Count && i < districts.Count; i++)
                {
                    var model = batchModels[i];
                    if (model.Hourly is not null)
                    {
                        var airQualityData = new AirQualityData
                        {
                            Latitude = model.Latitude,
                            Longitude = model.Longitude,
                            Times = model.Hourly.Time,
                            Pm25Values = model.Hourly.Pm25
                        };
                        results.Add((districts[i], airQualityData));
                    }
                }
            }
        }

        return results;
    }

    private static string BuildCacheKey(double latitude, double longitude)
    {
        return $"{Constants.CacheKeys.AirQualityPrefix}:{latitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}:{longitude.ToString($"F{Constants.Defaults.CoordinatePrecision}")}";
    }

    private string BuildSingleUrl(double latitude, double longitude)
    {
        return $"{_apiSettings.AirQualityApiBaseUrl}?latitude={latitude}&longitude={longitude}&hourly={Constants.ApiParameters.HourlyPm25}&forecast_days={Constants.ApiParameters.ForecastDays}&timezone={Uri.EscapeDataString(Constants.ApiParameters.Timezone)}";
    }
}
