namespace TravelAdvisor.Application.Features.TravelRecommendation;

public sealed class GetTravelRecommendationQueryHandler(
    IDistrictService districtService,
    IWeatherService weatherService,
    IAirQualityService airQualityService,
    IMapper mapper) : IRequestHandler<GetTravelRecommendationQuery, TravelRecommendationResponse>
{
    public async Task<TravelRecommendationResponse> Handle(GetTravelRecommendationQuery request, CancellationToken cancellationToken)
    {
        var currentLocation = await districtService.GetNearestDistrictAsync(request.CurrentLatitude, request.CurrentLongitude, cancellationToken)
            ?? throw new InvalidOperationException("Unable to find nearest district for current location.");

        var destination = await districtService.GetDistrictByNameAsync(request.DestinationDistrict, cancellationToken)
            ?? throw new ArgumentException($"District '{request.DestinationDistrict}' not found.");

        var currentWeatherTask = weatherService.GetWeatherAsync(currentLocation.Latitude, currentLocation.Longitude, cancellationToken);
        var currentAirQualityTask = airQualityService.GetAirQualityAsync(currentLocation.Latitude, currentLocation.Longitude, cancellationToken);
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

        var isSameDistrict = currentLocation.Name.Equals(destination.Name, StringComparison.OrdinalIgnoreCase);
        var tempDifference = Math.Round(currentTemp.Value - destTemp.Value, 1);
        var isCooler = destTemp.Value < currentTemp.Value;
        var isBetterAir = destPm25.Value < currentPm25.Value;

        var (recommendation, reason) = GenerateRecommendation(isSameDistrict, isCooler, isBetterAir, tempDifference);

        var currentLocationDto = mapper.Map<LocationComparisonDto>(currentLocation) with
        {
            TemperatureAt2pm = Math.Round(currentTemp.Value, 2),
            Pm25Level = Math.Round(currentPm25.Value, 2)
        };

        var destinationDto = mapper.Map<LocationComparisonDto>(destination) with
        {
            TemperatureAt2pm = Math.Round(destTemp.Value, 2),
            Pm25Level = Math.Round(destPm25.Value, 2)
        };

        return new TravelRecommendationResponse
        {
            Recommendation = recommendation,
            Reason = reason,
            CurrentLocation = currentLocationDto,
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
        bool isSameDistrict,
        bool isCooler,
        bool isBetterAir,
        double tempDifference)
    {
        if (isSameDistrict)
        {
            return (Constants.Recommendations.Recommended, Constants.Recommendations.AlreadyNearDestination);
        }

        if (isCooler && isBetterAir)
        {
            return (Constants.Recommendations.Recommended,
                string.Format(Constants.Recommendations.CoolerAndBetterAirTemplate, Math.Abs(tempDifference)));
        }

        return (Constants.Recommendations.NotRecommended, Constants.Recommendations.HotterAndWorseAir);
    }
}
