using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.BackgroundJobs;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.Persistence;
using TravelAdvisor.Tests.Common;

namespace TravelAdvisor.Tests.BackgroundJobs;

public class DistrictSyncJobTests : IDisposable
{
    private readonly TravelAdvisorDbContext _dbContext;
    private readonly Mock<ILogger<DistrictSyncJob>> _loggerMock;
    private readonly IOptions<ApiSettings> _apiSettings;

    public DistrictSyncJobTests()
    {
        var options = new DbContextOptionsBuilder<TravelAdvisorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TravelAdvisorDbContext(options);

        _loggerMock = new Mock<ILogger<DistrictSyncJob>>();
        _apiSettings = Options.Create(new ApiSettings
        {
            DistrictsUrl = "https://api.example.com/districts",
            WeatherApiBaseUrl = "https://api.open-meteo.com/v1/forecast",
            AirQualityApiBaseUrl = "https://air-quality-api.open-meteo.com/v1/air-quality"
        });
    }

    [Fact]
    public async Task SyncDistrictsAsync_WhenDistrictsExist_ShouldSkipSync()
    {
        await _dbContext.Districts.AddAsync(new District
        {
            Id = "1",
            DivisionId = "1",
            Name = "Dhaka",
            BnName = "ঢাকা",
            Latitude = 23.8103,
            Longitude = 90.4125
        });
        await _dbContext.SaveChangesAsync();

        var httpClient = new HttpClient(new MockHttpMessageHandler("{}"));
        var job = CreateJob(httpClient);

        await job.SyncDistrictsAsync();

        var count = await _dbContext.Districts.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SyncDistrictsAsync_WhenNoDistricts_ShouldFetchAndSave()
    {
        var apiResponse = @"{
            ""districts"": [
                {
                    ""id"": ""1"",
                    ""division_id"": ""1"",
                    ""name"": ""Dhaka"",
                    ""bn_name"": ""ঢাকা"",
                    ""lat"": ""23.8103"",
                    ""long"": ""90.4125""
                },
                {
                    ""id"": ""2"",
                    ""division_id"": ""2"",
                    ""name"": ""Chittagong"",
                    ""bn_name"": ""চট্টগ্রাম"",
                    ""lat"": ""22.3569"",
                    ""long"": ""91.7832""
                }
            ]
        }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var job = CreateJob(httpClient);

        await job.SyncDistrictsAsync();

        var count = await _dbContext.Districts.CountAsync();
        count.Should().Be(2);

        var dhaka = await _dbContext.Districts.FirstOrDefaultAsync(d => d.Name == "Dhaka");
        dhaka.Should().NotBeNull();
        dhaka!.Latitude.Should().Be(23.8103);
    }

    [Fact]
    public async Task SyncDistrictsAsync_WhenApiReturnsEmptyList_ShouldNotAddDistricts()
    {
        var apiResponse = @"{ ""districts"": [] }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var job = CreateJob(httpClient);

        await job.SyncDistrictsAsync();

        var count = await _dbContext.Districts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SyncDistrictsAsync_WhenApiReturnsNull_ShouldNotAddDistricts()
    {
        var apiResponse = @"{ ""districts"": null }";

        var httpClient = new HttpClient(new MockHttpMessageHandler(apiResponse));
        var job = CreateJob(httpClient);

        await job.SyncDistrictsAsync();

        var count = await _dbContext.Districts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SyncDistrictsAsync_WhenApiFails_ShouldThrow()
    {
        var httpClient = new HttpClient(new FailingHttpMessageHandler());
        var job = CreateJob(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => job.SyncDistrictsAsync());
    }

    private DistrictSyncJob CreateJob(HttpClient httpClient)
    {
        return new DistrictSyncJob(
            httpClient,
            _dbContext,
            _apiSettings,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
