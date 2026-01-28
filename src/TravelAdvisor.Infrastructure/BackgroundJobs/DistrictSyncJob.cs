using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.ExternalApis.Models;
using TravelAdvisor.Infrastructure.Persistence;

namespace TravelAdvisor.Infrastructure.BackgroundJobs;

public sealed class DistrictSyncJob(
    HttpClient httpClient,
    TravelAdvisorDbContext dbContext,
    IOptions<ApiSettings> apiSettings,
    ILogger<DistrictSyncJob> logger)
{
    private readonly ApiSettings _apiSettings = apiSettings.Value;

    public async Task SyncDistrictsAsync()
    {
        logger.LogInformation("Starting district sync job");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var existingCount = await dbContext.Districts.CountAsync();
            if (existingCount > 0)
            {
                logger.LogInformation("Districts already exist in database. Count: {Count}", existingCount);
                stopwatch.Stop();
                return;
            }

            var response = await httpClient.GetStringAsync(_apiSettings.DistrictsUrl);
            var data = JsonSerializer.Deserialize<DistrictListApiModel>(response);

            if (data?.Districts is null || data.Districts.Count == 0)
            {
                logger.LogWarning("No districts found in API response");
                return;
            }

            var districts = data.Districts.Select(d => new District
            {
                Id = d.Id,
                DivisionId = d.DivisionId,
                Name = d.Name,
                BnName = d.BnName,
                Latitude = double.Parse(d.Lat),
                Longitude = double.Parse(d.Long)
            }).ToList();

            await dbContext.Districts.AddRangeAsync(districts);
            await dbContext.SaveChangesAsync();

            stopwatch.Stop();
            logger.LogInformation(
                "District sync completed. Added {Count} districts. Duration: {Duration}ms",
                districts.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "District sync failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
