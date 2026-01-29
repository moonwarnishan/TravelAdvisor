using AutoMapper;
using FluentAssertions;
using Moq;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Application.DTOs.Districts;
using TravelAdvisor.Application.Features.TravelRecommendation;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;

namespace TravelAdvisor.Tests.Handlers;

public class GetTravelRecommendationQueryHandlerTests
{
    private readonly Mock<IDistrictService> _districtServiceMock;
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<IAirQualityService> _airQualityServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetTravelRecommendationQueryHandler _handler;

    public GetTravelRecommendationQueryHandlerTests()
    {
        _districtServiceMock = new Mock<IDistrictService>();
        _weatherServiceMock = new Mock<IWeatherService>();
        _airQualityServiceMock = new Mock<IAirQualityService>();
        _mapperMock = new Mock<IMapper>();

        _handler = new GetTravelRecommendationQueryHandler(
            _districtServiceMock.Object,
            _weatherServiceMock.Object,
            _airQualityServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDestinationIsCoolerAndBetterAir_ShouldReturnRecommended()
    {
        var travelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = new GetTravelRecommendationQuery(23.8103, 90.4125, "Cox's Bazar", travelDate);

        var currentDistrict = CreateDistrict("Dhaka", 23.8103, 90.4125);
        var destinationDistrict = CreateDistrict("Cox's Bazar", 21.4272, 92.0058);

        SetupDistrictService(currentDistrict, destinationDistrict);
        SetupWeatherService(travelDate, 30.0, 25.0);
        SetupAirQualityService(travelDate, 80.0, 40.0);
        SetupMapper(currentDistrict, destinationDistrict);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Recommendation.Should().Be(Constants.Recommendations.Recommended);
        result.CurrentLocation.Name.Should().Be("Dhaka");
        result.Destination.Name.Should().Be("Cox's Bazar");
    }

    [Fact]
    public async Task Handle_WhenDestinationIsHotterAndWorseAir_ShouldReturnNotRecommended()
    {
        var travelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = new GetTravelRecommendationQuery(23.8103, 90.4125, "Rajshahi", travelDate);

        var currentDistrict = CreateDistrict("Dhaka", 23.8103, 90.4125);
        var destinationDistrict = CreateDistrict("Rajshahi", 24.3636, 88.6241);

        SetupDistrictService(currentDistrict, destinationDistrict);
        SetupWeatherService(travelDate, 25.0, 35.0);
        SetupAirQualityService(travelDate, 40.0, 100.0);
        SetupMapper(currentDistrict, destinationDistrict);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Recommendation.Should().Be(Constants.Recommendations.NotRecommended);
        result.Reason.Should().Be(Constants.Recommendations.HotterAndWorseAir);
    }

    [Fact]
    public async Task Handle_WhenSameDistrict_ShouldReturnAlreadyNearDestination()
    {
        var travelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = new GetTravelRecommendationQuery(23.8103, 90.4125, "Dhaka", travelDate);

        var district = CreateDistrict("Dhaka", 23.8103, 90.4125);

        SetupDistrictService(district, district);
        SetupWeatherService(travelDate, 30.0, 30.0);
        SetupAirQualityService(travelDate, 50.0, 50.0);
        SetupMapper(district, district);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Recommendation.Should().Be(Constants.Recommendations.Recommended);
        result.Reason.Should().Be(Constants.Recommendations.AlreadyNearDestination);
    }

    [Fact]
    public async Task Handle_WhenDistrictNotFound_ShouldThrowArgumentException()
    {
        var travelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = new GetTravelRecommendationQuery(23.8103, 90.4125, "InvalidDistrict", travelDate);

        var currentDistrict = CreateDistrict("Dhaka", 23.8103, 90.4125);

        _districtServiceMock
            .Setup(x => x.GetNearestDistrictAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentDistrict);

        _districtServiceMock
            .Setup(x => x.GetDistrictByNameAsync("InvalidDistrict", It.IsAny<CancellationToken>()))
            .ReturnsAsync((District?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNearestDistrictNotFound_ShouldThrowInvalidOperationException()
    {
        var travelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = new GetTravelRecommendationQuery(0, 0, "Dhaka", travelDate);

        _districtServiceMock
            .Setup(x => x.GetNearestDistrictAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((District?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, CancellationToken.None));
    }

    private static District CreateDistrict(string name, double lat, double lon) => new()
    {
        Id = Guid.NewGuid().ToString(),
        DivisionId = "1",
        Name = name,
        BnName = name,
        Latitude = lat,
        Longitude = lon
    };

    private void SetupDistrictService(District currentDistrict, District destinationDistrict)
    {
        _districtServiceMock
            .Setup(x => x.GetNearestDistrictAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentDistrict);

        _districtServiceMock
            .Setup(x => x.GetDistrictByNameAsync(destinationDistrict.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationDistrict);
    }

    private void SetupWeatherService(DateOnly travelDate, double currentTemp, double destTemp)
    {
        var timeString = $"{travelDate:yyyy-MM-dd}T14:00";

        _weatherServiceMock
            .Setup(x => x.GetWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((double lat, double lon, CancellationToken _) => new WeatherData
            {
                Latitude = lat,
                Longitude = lon,
                Times = new[] { timeString },
                Temperatures = new double?[] { lat < 22 ? destTemp : currentTemp }
            });
    }

    private void SetupAirQualityService(DateOnly travelDate, double currentPm25, double destPm25)
    {
        var timeString = $"{travelDate:yyyy-MM-dd}T14:00";

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((double lat, double lon, CancellationToken _) => new AirQualityData
            {
                Latitude = lat,
                Longitude = lon,
                Times = new[] { timeString },
                Pm25Values = new double?[] { lat < 22 ? destPm25 : currentPm25 }
            });
    }

    private void SetupMapper(District currentDistrict, District destinationDistrict)
    {
        _mapperMock
            .Setup(x => x.Map<LocationComparisonDto>(currentDistrict))
            .Returns(new LocationComparisonDto
            {
                Name = currentDistrict.Name,
                Latitude = currentDistrict.Latitude,
                Longitude = currentDistrict.Longitude
            });

        _mapperMock
            .Setup(x => x.Map<LocationComparisonDto>(destinationDistrict))
            .Returns(new LocationComparisonDto
            {
                Name = destinationDistrict.Name,
                Latitude = destinationDistrict.Latitude,
                Longitude = destinationDistrict.Longitude
            });
    }
}
