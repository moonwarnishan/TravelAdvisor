namespace TravelAdvisor.Infrastructure.BackgroundJobs;

public sealed class CacheWarmingJob(
    IDistrictService districtService,
    IWeatherService weatherService,
    IAirQualityService airQualityService,
    ICacheService cacheService,
    ILogger<CacheWarmingJob> logger)
{
    public async Task WarmupCacheAsync()
    {
        logger.LogInformation("Starting cache warmup job");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var districts = await districtService.GetAllDistrictsAsync();
            var districtList = districts.ToList();

            logger.LogInformation("Fetched {Count} districts, warming weather and air quality cache", districtList.Count);

            var weatherTask = weatherService.GetWeatherForDistrictsAsync(districtList);
            var airQualityTask = airQualityService.GetAirQualityForDistrictsAsync(districtList);

            await Task.WhenAll(weatherTask, airQualityTask);

            var weatherData = await weatherTask;
            var airQualityData = await airQualityTask;

            await CacheTopDistrictsRankingAsync(districtList, weatherData, airQualityData);

            stopwatch.Stop();
            logger.LogInformation(
                "Cache warmup completed successfully. Weather: {WeatherCount}, AirQuality: {AirQualityCount}. Duration: {Duration}ms",
                weatherData.Count,
                airQualityData.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Cache warmup failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task CacheTopDistrictsRankingAsync(
        List<District> districts,
        Dictionary<string, WeatherData> weatherData,
        Dictionary<string, AirQualityData> airQualityData)
    {
        var rankedDistricts = new List<(District District, double AvgTemp, double AvgPm25)>();

        foreach (var district in districts)
        {
            if (!weatherData.TryGetValue(district.Name, out var weather) ||
                !airQualityData.TryGetValue(district.Name, out var airQuality))
            {
                continue;
            }

            var avgTemp = CalculateAverageAt2pm(weather.Times, weather.Temperatures);
            var avgPm25 = CalculateAverageAt2pm(airQuality.Times, airQuality.Pm25Values);

            if (avgTemp.HasValue && avgPm25.HasValue)
            {
                rankedDistricts.Add((district, avgTemp.Value, avgPm25.Value));
            }
        }

        var topDistricts = rankedDistricts
            .OrderBy(x => x.AvgTemp)
            .ThenBy(x => x.AvgPm25)
            .Take(Constants.Defaults.TopDistrictsCount)
            .ToList();

        if (topDistricts.Count > 0)
        {
            var cacheData = new CachedTopDistrictsData
            {
                Rankings = topDistricts.Select((x, index) => new CachedDistrictRanking
                {
                    Rank = index + 1,
                    DistrictName = x.District.Name,
                    Latitude = x.District.Latitude,
                    Longitude = x.District.Longitude,
                    AverageTemperature = Math.Round(x.AvgTemp, 2),
                    AveragePm25 = Math.Round(x.AvgPm25, 2)
                }).ToList(),
                GeneratedAt = DateTime.UtcNow,
                ForecastPeriod = $"{DateTime.UtcNow.ToString(Constants.TimeFormats.DateFormat)} to {DateTime.UtcNow.AddDays(Constants.ApiParameters.ForecastDays - 1).ToString(Constants.TimeFormats.DateFormat)}"
            };

            await cacheService.SetAsync(
                Constants.CacheKeys.TopDistrictsRanking,
                cacheData,
                TimeSpan.FromMinutes(30));

            logger.LogInformation("Cached top {Count} districts ranking", topDistricts.Count);
        }
    }

    private static double? CalculateAverageAt2pm(IReadOnlyList<string> times, IReadOnlyList<double?> values)
    {
        if (times.Count == 0 || values.Count == 0)
            return null;

        var valuesAt2pm = new List<double>();
        for (var i = 0; i < times.Count && i < values.Count; i++)
        {
            if (times[i].Contains(Constants.TimeFormats.TwoPmSuffix) && values[i].HasValue)
            {
                valuesAt2pm.Add(values[i]!.Value);
            }
        }

        return valuesAt2pm.Count > 0 ? valuesAt2pm.Average() : null;
    }
}

public sealed class CachedTopDistrictsData
{
    public required List<CachedDistrictRanking> Rankings { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required string ForecastPeriod { get; init; }
}

public sealed class CachedDistrictRanking
{
    public required int Rank { get; init; }
    public required string DistrictName { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required double AverageTemperature { get; init; }
    public required double AveragePm25 { get; init; }
}
