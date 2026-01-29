using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Application.DTOs.Districts;
using TravelAdvisor.Application.Features.TopDistricts;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;

namespace TravelAdvisor.Tests.Handlers;

public class GetTopDistrictsQueryHandlerTests
{
    private readonly Mock<IDistrictService> _districtServiceMock;
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<IAirQualityService> _airQualityServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<GetTopDistrictsQueryHandler>> _loggerMock;
    private readonly GetTopDistrictsQueryHandler _handler;

    public GetTopDistrictsQueryHandlerTests()
    {
        _districtServiceMock = new Mock<IDistrictService>();
        _weatherServiceMock = new Mock<IWeatherService>();
        _airQualityServiceMock = new Mock<IAirQualityService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<GetTopDistrictsQueryHandler>>();

        _handler = new GetTopDistrictsQueryHandler(
            _districtServiceMock.Object,
            _weatherServiceMock.Object,
            _airQualityServiceMock.Object,
            _cacheServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheExists_ShouldReturnCachedResponse()
    {
        var cachedResponse = new TopDistrictsResponse
        {
            Districts = new List<RankedDistrictDto>
            {
                new() { Rank = 1, Name = "Sylhet", Latitude = 24.8949, Longitude = 91.8687 }
            },
            GeneratedAt = DateTime.UtcNow,
            ForecastPeriod = "2026-01-30 to 2026-02-05"
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<TopDistrictsResponse>(Constants.CacheKeys.TopDistrictsRanking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var query = new GetTopDistrictsQuery(5);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Be(cachedResponse);
        _districtServiceMock.Verify(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoCacheExists_ShouldFetchAndRankDistricts()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<TopDistrictsResponse>(Constants.CacheKeys.TopDistrictsRanking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopDistrictsResponse?)null);

        var districts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Sylhet", 24.8949, 91.8687),
            CreateDistrict("Cox's Bazar", 21.4272, 92.0058)
        };

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        var weatherData = CreateWeatherData(districts);
        var airQualityData = CreateAirQualityData(districts);

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        SetupMapper(districts);

        var query = new GetTopDistrictsQuery(3);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Districts.Should().HaveCount(3);
        result.Districts.First().Rank.Should().Be(1);

        _cacheServiceMock.Verify(
            x => x.SetAsync(Constants.CacheKeys.TopDistrictsRanking, It.IsAny<TopDistrictsResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldOrderByCoolestThenBestAirQuality()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<TopDistrictsResponse>(Constants.CacheKeys.TopDistrictsRanking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopDistrictsResponse?)null);

        var districts = new List<District>
        {
            CreateDistrict("Hot", 23.0, 90.0),
            CreateDistrict("Cool", 24.0, 91.0),
            CreateDistrict("Coolest", 25.0, 92.0)
        };

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        var weatherData = new Dictionary<string, WeatherData>
        {
            ["Hot"] = CreateWeatherDataForDistrict(35.0),
            ["Cool"] = CreateWeatherDataForDistrict(25.0),
            ["Coolest"] = CreateWeatherDataForDistrict(20.0)
        };

        var airQualityData = new Dictionary<string, AirQualityData>
        {
            ["Hot"] = CreateAirQualityDataForDistrict(100.0),
            ["Cool"] = CreateAirQualityDataForDistrict(50.0),
            ["Coolest"] = CreateAirQualityDataForDistrict(30.0)
        };

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        SetupMapper(districts);

        var query = new GetTopDistrictsQuery(3);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Districts[0].Name.Should().Be("Coolest");
        result.Districts[1].Name.Should().Be("Cool");
        result.Districts[2].Name.Should().Be("Hot");
    }

    [Fact]
    public async Task Handle_WithCountLessThanDistricts_ShouldReturnRequestedCount()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<TopDistrictsResponse>(Constants.CacheKeys.TopDistrictsRanking, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopDistrictsResponse?)null);

        var districts = new List<District>
        {
            CreateDistrict("District1", 23.0, 90.0),
            CreateDistrict("District2", 24.0, 91.0),
            CreateDistrict("District3", 25.0, 92.0),
            CreateDistrict("District4", 22.0, 89.0),
            CreateDistrict("District5", 26.0, 91.5)
        };

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        var weatherData = CreateWeatherData(districts);
        var airQualityData = CreateAirQualityData(districts);

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        SetupMapper(districts);

        var query = new GetTopDistrictsQuery(2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Districts.Should().HaveCount(2);
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

    private static Dictionary<string, WeatherData> CreateWeatherData(List<District> districts)
    {
        var result = new Dictionary<string, WeatherData>();
        var random = new Random(42);

        foreach (var district in districts)
        {
            result[district.Name] = CreateWeatherDataForDistrict(20 + random.NextDouble() * 15);
        }

        return result;
    }

    private static WeatherData CreateWeatherDataForDistrict(double temperature)
    {
        var times = Enumerable.Range(0, 7)
            .Select(i => $"{DateTime.UtcNow.AddDays(i):yyyy-MM-dd}T14:00")
            .ToList();

        return new WeatherData
        {
            Latitude = 23.0,
            Longitude = 90.0,
            Times = times,
            Temperatures = times.Select(_ => (double?)temperature).ToList()
        };
    }

    private static Dictionary<string, AirQualityData> CreateAirQualityData(List<District> districts)
    {
        var result = new Dictionary<string, AirQualityData>();
        var random = new Random(42);

        foreach (var district in districts)
        {
            result[district.Name] = CreateAirQualityDataForDistrict(20 + random.NextDouble() * 80);
        }

        return result;
    }

    private static AirQualityData CreateAirQualityDataForDistrict(double pm25)
    {
        var times = Enumerable.Range(0, 7)
            .Select(i => $"{DateTime.UtcNow.AddDays(i):yyyy-MM-dd}T14:00")
            .ToList();

        return new AirQualityData
        {
            Latitude = 23.0,
            Longitude = 90.0,
            Times = times,
            Pm25Values = times.Select(_ => (double?)pm25).ToList()
        };
    }

    private void SetupMapper(List<District> districts)
    {
        foreach (var district in districts)
        {
            _mapperMock
                .Setup(x => x.Map<RankedDistrictDto>(district))
                .Returns(new RankedDistrictDto
                {
                    Name = district.Name,
                    Latitude = district.Latitude,
                    Longitude = district.Longitude
                });
        }
    }
}
