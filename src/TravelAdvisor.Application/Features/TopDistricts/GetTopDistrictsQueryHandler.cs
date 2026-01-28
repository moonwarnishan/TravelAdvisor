namespace TravelAdvisor.Application.Features.TopDistricts;

public sealed class GetTopDistrictsQueryHandler(
    IDistrictService districtService,
    IWeatherService weatherService,
    IAirQualityService airQualityService,
    ICacheService cacheService,
    IMapper mapper,
    ILogger<GetTopDistrictsQueryHandler> logger) : IRequestHandler<GetTopDistrictsQuery, TopDistrictsResponse>
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public async Task<TopDistrictsResponse> Handle(GetTopDistrictsQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacheService.GetAsync<TopDistrictsResponse>(Constants.CacheKeys.TopDistrictsRanking, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Retrieved top districts from cache");
            return cached;
        }

        var districts = await districtService.GetAllDistrictsAsync(cancellationToken);
        var districtList = districts.ToList();

        var weatherTask = weatherService.GetWeatherForDistrictsAsync(districtList, cancellationToken);
        var airQualityTask = airQualityService.GetAirQualityForDistrictsAsync(districtList, cancellationToken);

        await Task.WhenAll(weatherTask, airQualityTask);

        var weatherData = await weatherTask;
        var airQualityData = await airQualityTask;

        var rankedDistricts = new List<(District District, double AvgTemp, double AvgPm25)>();

        foreach (var district in districtList)
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

        var sorted = rankedDistricts
            .OrderBy(x => x.AvgTemp)
            .ThenBy(x => x.AvgPm25)
            .Take(request.Count)
            .Select((x, index) =>
            {
                var dto = mapper.Map<RankedDistrictDto>(x.District);
                return dto with
                {
                    Rank = index + 1,
                    AverageTemperatureAt2pm = Math.Round(x.AvgTemp, 2),
                    AveragePm25 = Math.Round(x.AvgPm25, 2)
                };
            })
            .ToList();

        var response = new TopDistrictsResponse
        {
            Districts = sorted,
            GeneratedAt = DateTime.UtcNow,
            ForecastPeriod = $"{DateTime.UtcNow.ToString(Constants.TimeFormats.DateFormat)} to {DateTime.UtcNow.AddDays(Constants.ApiParameters.ForecastDays - 1).ToString(Constants.TimeFormats.DateFormat)}"
        };

        await cacheService.SetAsync(Constants.CacheKeys.TopDistrictsRanking, response, CacheExpiration, cancellationToken);

        return response;
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
