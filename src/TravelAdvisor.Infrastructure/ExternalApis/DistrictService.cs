using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Infrastructure.Persistence;

namespace TravelAdvisor.Infrastructure.ExternalApis;

public sealed class DistrictService(
    HttpClient httpClient,
    ICacheService cacheService,
    TravelAdvisorDbContext dbContext,
    IMapper mapper,
    IOptions<ApiSettings> apiSettings,
    ILogger<DistrictService> logger) : IDistrictService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);
    private readonly ApiSettings _apiSettings = apiSettings.Value;

    public async Task<IReadOnlyList<District>> GetAllDistrictsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cacheService.GetAsync<List<District>>(Constants.CacheKeys.AllDistricts, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Retrieved districts from cache");
            return cached;
        }

        var dbDistricts = await dbContext.Districts.ToListAsync(cancellationToken);
        if (dbDistricts.Count > 0)
        {
            logger.LogDebug("Retrieved {Count} districts from database", dbDistricts.Count);
            await cacheService.SetAsync(Constants.CacheKeys.AllDistricts, dbDistricts, CacheExpiration, cancellationToken);
            return dbDistricts;
        }

        logger.LogInformation("Fetching districts from remote source");
        var response = await httpClient.GetStringAsync(_apiSettings.DistrictsUrl, cancellationToken);
        var data = JsonSerializer.Deserialize<DistrictListApiModel>(response);

        if (data?.Districts is null || data.Districts.Count == 0)
        {
            logger.LogWarning("No districts found in response");
            return [];
        }

        var districts = mapper.Map<List<District>>(data.Districts);

        await dbContext.Districts.AddRangeAsync(districts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Saved {Count} districts to database", districts.Count);

        await cacheService.SetAsync(Constants.CacheKeys.AllDistricts, districts, CacheExpiration, cancellationToken);
        return districts;
    }

    public async Task<District?> GetDistrictByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var districts = await GetAllDistrictsAsync(cancellationToken);
        return districts.FirstOrDefault(d =>
            d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            d.BnName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<District?> GetNearestDistrictAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var districts = await GetAllDistrictsAsync(cancellationToken);
        if (districts.Count == 0)
            return null;

        return districts
            .Select(d => new { District = d, Distance = CalculateDistance(latitude, longitude, d.Latitude, d.Longitude) })
            .MinBy(x => x.Distance)?.District;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
