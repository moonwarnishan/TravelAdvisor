namespace TravelAdvisor.Application.Common.Interfaces;

public interface IDistrictService
{
    Task<IReadOnlyList<District>> GetAllDistrictsAsync(CancellationToken cancellationToken = default);
    Task<District?> GetDistrictByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<District?> GetNearestDistrictAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
