using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.ExternalApis;
using TravelAdvisor.Tests.Common;

namespace TravelAdvisor.Tests.Services;

public class AirQualityServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<AirQualityService>> _loggerMock;
    private readonly IOptions<ApiSettings> _apiSettings;

    public AirQualityServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AirQualityService>>();
        _apiSettings = Options.Create(new ApiSettings
        {
            DistrictsUrl = "https://api.example.com/districts",
            WeatherApiBaseUrl = "https://api.open-meteo.com/v1/forecast",
            AirQualityApiBaseUrl = "https://air-quality-api.open-meteo.com/v1/air-quality"
        });
    }

    [Fact]
    public async Task GetAirQualityAsync_WhenCached_ShouldReturnCachedData()
    {
        var cachedData = new AirQualityData
        {
            Latitude = 23.8103,
            Longitude = 90.4125,
            Times = new[] { "2026-01-30T14:00" },
            Pm25Values = new double?[] { 45.5 }
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityAsync(23.8103, 90.4125);

        result.Should().BeEquivalentTo(cachedData);
    }

    [Fact]
    public async Task GetAirQualityAsync_WhenNotCached_ShouldFetchFromApi()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualityData?)null);

        var apiResponse = @"{
            ""latitude"": 23.81,
            ""longitude"": 90.41,
            ""hourly"": {
                ""time"": [""2026-01-30T14:00""],
                ""pm2_5"": [55.2]
            }
        }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityAsync(23.8103, 90.4125);

        result.Should().NotBeNull();
        result!.Pm25Values.Should().Contain(55.2);

        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<AirQualityData>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAirQualityAsync_WhenNullHourly_ShouldReturnNull()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualityData?)null);

        var apiResponse = @"{
            ""latitude"": 23.81,
            ""longitude"": 90.41
        }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityAsync(23.8103, 90.4125);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAirQualityAsync_WhenApiFails_ShouldReturnNull()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualityData?)null);

        var httpClient = new HttpClient(new FailingHttpMessageHandler());
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityAsync(23.8103, 90.4125);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAirQualityForDistrictsAsync_WhenAllCached_ShouldReturnCachedData()
    {
        var districts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Sylhet", 24.8949, 91.8687)
        };

        var airQualityData = new AirQualityData
        {
            Latitude = 23.8103,
            Longitude = 90.4125,
            Times = new[] { "2026-01-30T14:00" },
            Pm25Values = new double?[] { 45.5 }
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityForDistrictsAsync(districts);

        result.Should().HaveCount(2);
        result.Should().ContainKey("Dhaka");
        result.Should().ContainKey("Sylhet");
    }

    [Fact]
    public async Task GetAirQualityForDistrictsAsync_WhenNotCached_ShouldFetchBatch()
    {
        var districts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<AirQualityData>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AirQualityData?)null);

        var apiResponse = @"{
            ""latitude"": 23.81,
            ""longitude"": 90.41,
            ""hourly"": {
                ""time"": [""2026-01-30T14:00""],
                ""pm2_5"": [42.8]
            }
        }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var service = CreateService(httpClient);

        var result = await service.GetAirQualityForDistrictsAsync(districts);

        result.Should().ContainKey("Dhaka");
        result["Dhaka"].Pm25Values.Should().Contain(42.8);
    }

    private AirQualityService CreateService(HttpClient httpClient)
    {
        return new AirQualityService(
            httpClient,
            _cacheServiceMock.Object,
            _apiSettings,
            _loggerMock.Object);
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
}
