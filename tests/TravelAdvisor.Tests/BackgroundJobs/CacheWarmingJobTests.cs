using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.BackgroundJobs;

namespace TravelAdvisor.Tests.BackgroundJobs;

public class CacheWarmingJobTests
{
    private readonly Mock<IDistrictService> _districtServiceMock;
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<IAirQualityService> _airQualityServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<CacheWarmingJob>> _loggerMock;
    private readonly CacheWarmingJob _job;

    public CacheWarmingJobTests()
    {
        _districtServiceMock = new Mock<IDistrictService>();
        _weatherServiceMock = new Mock<IWeatherService>();
        _airQualityServiceMock = new Mock<IAirQualityService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<CacheWarmingJob>>();

        _job = new CacheWarmingJob(
            _districtServiceMock.Object,
            _weatherServiceMock.Object,
            _airQualityServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task WarmupCacheAsync_ShouldFetchDistrictsAndWarmCache()
    {
        var districts = CreateDistricts();
        var weatherData = CreateWeatherData(districts);
        var airQualityData = CreateAirQualityData(districts);

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        await _job.WarmupCacheAsync();

        _districtServiceMock.Verify(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _weatherServiceMock.Verify(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()), Times.Once);
        _airQualityServiceMock.Verify(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmupCacheAsync_ShouldCacheTopDistrictsRanking()
    {
        var districts = CreateDistricts();
        var weatherData = CreateWeatherData(districts);
        var airQualityData = CreateAirQualityData(districts);

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(airQualityData);

        await _job.WarmupCacheAsync();

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                Constants.CacheKeys.TopDistrictsRanking,
                It.IsAny<CachedTopDistrictsData>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WarmupCacheAsync_WhenDistrictServiceFails_ShouldThrow()
    {
        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        await Assert.ThrowsAsync<Exception>(() => _job.WarmupCacheAsync());
    }

    [Fact]
    public async Task WarmupCacheAsync_WithEmptyDistricts_ShouldNotCacheRanking()
    {
        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<District>());

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, WeatherData>());

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, AirQualityData>());

        await _job.WarmupCacheAsync();

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                Constants.CacheKeys.TopDistrictsRanking,
                It.IsAny<CachedTopDistrictsData>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WarmupCacheAsync_ShouldFetchWeatherAndAirQualityInParallel()
    {
        var districts = CreateDistricts();
        var weatherTaskCompletionSource = new TaskCompletionSource<Dictionary<string, WeatherData>>();
        var airQualityTaskCompletionSource = new TaskCompletionSource<Dictionary<string, AirQualityData>>();

        _districtServiceMock
            .Setup(x => x.GetAllDistrictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(districts);

        _weatherServiceMock
            .Setup(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .Returns(weatherTaskCompletionSource.Task);

        _airQualityServiceMock
            .Setup(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()))
            .Returns(airQualityTaskCompletionSource.Task);

        var warmupTask = _job.WarmupCacheAsync();

        await Task.Delay(50);

        _weatherServiceMock.Verify(x => x.GetWeatherForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()), Times.Once);
        _airQualityServiceMock.Verify(x => x.GetAirQualityForDistrictsAsync(It.IsAny<IEnumerable<District>>(), It.IsAny<CancellationToken>()), Times.Once);

        weatherTaskCompletionSource.SetResult(CreateWeatherData(districts));
        airQualityTaskCompletionSource.SetResult(CreateAirQualityData(districts));

        await warmupTask;
    }

    private static List<District> CreateDistricts() => new()
    {
        new() { Id = "1", DivisionId = "1", Name = "Dhaka", BnName = "ঢাকা", Latitude = 23.8103, Longitude = 90.4125 },
        new() { Id = "2", DivisionId = "2", Name = "Sylhet", BnName = "সিলেট", Latitude = 24.8949, Longitude = 91.8687 },
        new() { Id = "3", DivisionId = "3", Name = "Cox's Bazar", BnName = "কক্সবাজার", Latitude = 21.4272, Longitude = 92.0058 }
    };

    private static Dictionary<string, WeatherData> CreateWeatherData(List<District> districts)
    {
        var result = new Dictionary<string, WeatherData>();
        foreach (var district in districts)
        {
            result[district.Name] = new WeatherData
            {
                Latitude = district.Latitude,
                Longitude = district.Longitude,
                Times = new[] { $"{DateTime.UtcNow:yyyy-MM-dd}T14:00" },
                Temperatures = new double?[] { 25.0 + districts.IndexOf(district) }
            };
        }
        return result;
    }

    private static Dictionary<string, AirQualityData> CreateAirQualityData(List<District> districts)
    {
        var result = new Dictionary<string, AirQualityData>();
        foreach (var district in districts)
        {
            result[district.Name] = new AirQualityData
            {
                Latitude = district.Latitude,
                Longitude = district.Longitude,
                Times = new[] { $"{DateTime.UtcNow:yyyy-MM-dd}T14:00" },
                Pm25Values = new double?[] { 30.0 + districts.IndexOf(district) * 10 }
            };
        }
        return result;
    }
}
