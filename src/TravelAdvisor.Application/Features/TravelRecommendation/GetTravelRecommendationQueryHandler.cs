namespace TravelAdvisor.Application.Features.TravelRecommendation;

public sealed class GetTravelRecommendationQueryHandler(
    IDistrictService districtService,
    IWeatherService weatherService,
    IAirQualityService airQualityService,
    IMapper mapper) : IRequestHandler<GetTravelRecommendationQuery, TravelRecommendationResponse>
{
    public async Task<TravelRecommendationResponse> Handle(GetTravelRecommendationQuery request, CancellationToken cancellationToken)
    {
        var destination = await districtService.GetDistrictByNameAsync(request.DestinationDistrict, cancellationToken)
            ?? throw new ArgumentException($"District '{request.DestinationDistrict}' not found.");

        var currentWeatherTask = weatherService.GetWeatherAsync(request.CurrentLatitude, request.CurrentLongitude, cancellationToken);
        var currentAirQualityTask = airQualityService.GetAirQualityAsync(request.CurrentLatitude, request.CurrentLongitude, cancellationToken);
        var destWeatherTask = weatherService.GetWeatherAsync(destination.Latitude, destination.Longitude, cancellationToken);
        var destAirQualityTask = airQualityService.GetAirQualityAsync(destination.Latitude, destination.Longitude, cancellationToken);

        await Task.WhenAll(currentWeatherTask, currentAirQualityTask, destWeatherTask, destAirQualityTask);

        var currentWeather = await currentWeatherTask ?? throw new InvalidOperationException("Unable to fetch weather data for current location.");
        var currentAirQuality = await currentAirQualityTask ?? throw new InvalidOperationException("Unable to fetch air quality data for current location.");
        var destWeather = await destWeatherTask ?? throw new InvalidOperationException("Unable to fetch weather data for destination.");
        var destAirQuality = await destAirQualityTask ?? throw new InvalidOperationException("Unable to fetch air quality data for destination.");

        var currentTemp = GetValueAt2pmForDate(currentWeather.Times, currentWeather.Temperatures, request.TravelDate);
        var currentPm25 = GetValueAt2pmForDate(currentAirQuality.Times, currentAirQuality.Pm25Values, request.TravelDate);
        var destTemp = GetValueAt2pmForDate(destWeather.Times, destWeather.Temperatures, request.TravelDate);
        var destPm25 = GetValueAt2pmForDate(destAirQuality.Times, destAirQuality.Pm25Values, request.TravelDate);

        if (!currentTemp.HasValue || !destTemp.HasValue || !currentPm25.HasValue || !destPm25.HasValue)
        {
            throw new InvalidOperationException("Weather data not available for the specified travel date.");
        }

        var isCooler = destTemp.Value < currentTemp.Value;
        var isBetterAir = destPm25.Value < currentPm25.Value;
        var tempDifference = Math.Round(currentTemp.Value - destTemp.Value, 1);

        var (recommendation, reason) = GenerateRecommendation(isCooler, isBetterAir, tempDifference, currentPm25.Value, destPm25.Value);

        var destinationDto = mapper.Map<LocationComparisonDto>(destination) with
        {
            TemperatureAt2pm = Math.Round(destTemp.Value, 2),
            Pm25Level = Math.Round(destPm25.Value, 2)
        };

        return new TravelRecommendationResponse
        {
            Recommendation = recommendation,
            Reason = reason,
            CurrentLocation = new LocationComparisonDto
            {
                Name = "Current Location",
                Latitude = request.CurrentLatitude,
                Longitude = request.CurrentLongitude,
                TemperatureAt2pm = Math.Round(currentTemp.Value, 2),
                Pm25Level = Math.Round(currentPm25.Value, 2)
            },
            Destination = destinationDto
        };
    }

    private static double? GetValueAt2pmForDate(IReadOnlyList<string> times, IReadOnlyList<double?> values, DateOnly date)
    {
        var targetTime = $"{date.ToString(Constants.TimeFormats.DateFormat)}{Constants.TimeFormats.TwoPmSuffix}";

        for (var i = 0; i < times.Count && i < values.Count; i++)
        {
            if (times[i] == targetTime && values[i].HasValue)
            {
                return values[i]!.Value;
            }
        }

        return null;
    }

    private static (string Recommendation, string Reason) GenerateRecommendation(
        bool isCooler,
        bool isBetterAir,
        double tempDifference,
        double currentPm25,
        double destPm25)
    {
        if (isCooler && isBetterAir)
        {
            var airQualityDesc = destPm25 < currentPm25 * 0.7 ? "significantly better" : "better";
            return (Constants.Recommendations.Recommended,
                $"Your destination is {Math.Abs(tempDifference)}°C cooler and has {airQualityDesc} air quality. Enjoy your trip!");
        }

        if (isCooler && !isBetterAir)
        {
            var pm25Diff = destPm25 - currentPm25;
            if (pm25Diff < 10)
            {
                return (Constants.Recommendations.Recommended,
                    $"Your destination is {Math.Abs(tempDifference)}°C cooler with slightly higher PM2.5 levels. The temperature benefit may outweigh the minor air quality difference.");
            }
            return (Constants.Recommendations.NotRecommended,
                $"Your destination is {Math.Abs(tempDifference)}°C cooler but has worse air quality (PM2.5: {destPm25:F1} vs {currentPm25:F1}). Consider the air quality trade-off.");
        }

        if (!isCooler && isBetterAir)
        {
            if (Math.Abs(tempDifference) < 2)
            {
                return (Constants.Recommendations.Recommended,
                    "Your destination has better air quality with similar temperature. A good choice for cleaner air!");
            }
            return (Constants.Recommendations.NotRecommended,
                $"Your destination is {Math.Abs(tempDifference)}°C warmer but has better air quality. Consider if the air quality improvement is worth the higher temperature.");
        }

        return (Constants.Recommendations.NotRecommended,
            "Your destination is hotter and has worse air quality than your current location. It's better to stay where you are.");
    }
}
