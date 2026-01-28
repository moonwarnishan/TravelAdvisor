using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Application.Common.Interfaces;
using TravelAdvisor.Domain.Common;
using TravelAdvisor.Domain.Entities;
using TravelAdvisor.Infrastructure.Configuration;
using TravelAdvisor.Infrastructure.ExternalApis.Models;

namespace TravelAdvisor.Infrastructure.ExternalApis;

public sealed class DistrictService(
    HttpClient httpClient,
    ICacheService cacheService,
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

        logger.LogInformation("Fetching districts from remote source");
        var response = await httpClient.GetStringAsync(_apiSettings.DistrictsUrl, cancellationToken);
        var data = JsonSerializer.Deserialize<DistrictListApiModel>(response);

        if (data?.Districts is null || data.Districts.Count == 0)
        {
            logger.LogWarning("No districts found in response");
            return [];
        }

        var districts = mapper.Map<List<District>>(data.Districts);

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
}
