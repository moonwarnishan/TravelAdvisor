using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.ExternalApis;
using TravelAdvisor.Infrastructure.Persistence;
using TravelAdvisor.Tests.Common;

namespace TravelAdvisor.Tests.Services;

public class DistrictServiceTests : IDisposable
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<DistrictService>> _loggerMock;
    private readonly TravelAdvisorDbContext _dbContext;
    private readonly IOptions<ApiSettings> _apiSettings;

    public DistrictServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<DistrictService>>();

        var options = new DbContextOptionsBuilder<TravelAdvisorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TravelAdvisorDbContext(options);

        _apiSettings = Options.Create(new ApiSettings
        {
            DistrictsUrl = "https://api.example.com/districts",
            WeatherApiBaseUrl = "https://api.open-meteo.com/v1/forecast",
            AirQualityApiBaseUrl = "https://air-quality-api.open-meteo.com/v1/air-quality"
        });
    }

    [Fact]
    public async Task GetAllDistrictsAsync_WhenCached_ShouldReturnCachedData()
    {
        var cachedDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Sylhet", 24.8949, 91.8687)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDistricts);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetAllDistrictsAsync();

        result.Should().BeEquivalentTo(cachedDistricts);
    }

    [Fact]
    public async Task GetAllDistrictsAsync_WhenInDatabase_ShouldReturnDatabaseData()
    {
        var dbDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Chittagong", 22.3569, 91.7832)
        };

        await _dbContext.Districts.AddRangeAsync(dbDistricts);
        await _dbContext.SaveChangesAsync();

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<District>?)null);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetAllDistrictsAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Dhaka");
        result.Should().Contain(d => d.Name == "Chittagong");

        _cacheServiceMock.Verify(
            x => x.SetAsync(Constants.CacheKeys.AllDistricts, It.IsAny<List<District>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDistrictByNameAsync_WhenExists_ShouldReturnDistrict()
    {
        var cachedDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Sylhet", 24.8949, 91.8687)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDistricts);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetDistrictByNameAsync("Dhaka");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Dhaka");
    }

    [Fact]
    public async Task GetDistrictByNameAsync_WhenNotExists_ShouldReturnNull()
    {
        var cachedDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDistricts);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetDistrictByNameAsync("NonExistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDistrictByNameAsync_ShouldBeCaseInsensitive()
    {
        var cachedDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDistricts);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetDistrictByNameAsync("dhaka");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Dhaka");
    }

    [Fact]
    public async Task GetNearestDistrictAsync_ShouldReturnNearestDistrict()
    {
        var cachedDistricts = new List<District>
        {
            CreateDistrict("Dhaka", 23.8103, 90.4125),
            CreateDistrict("Sylhet", 24.8949, 91.8687),
            CreateDistrict("Cox's Bazar", 21.4272, 92.0058)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDistricts);

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var service = CreateService(httpClient);

        var result = await service.GetNearestDistrictAsync(23.8, 90.4);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Dhaka");
    }

    [Fact]
    public async Task GetNearestDistrictAsync_WhenNoDistricts_ShouldReturnNull()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<District>());

        var httpClient = new HttpClient(new MockHttpMessageHandler("{\"districts\":[]}"));
        var service = CreateService(httpClient);

        var result = await service.GetNearestDistrictAsync(23.8, 90.4);

        result.Should().BeNull();
    }

    private DistrictService CreateService(HttpClient httpClient)
    {
        return new DistrictService(
            httpClient,
            _cacheServiceMock.Object,
            _dbContext,
            _mapperMock.Object,
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
