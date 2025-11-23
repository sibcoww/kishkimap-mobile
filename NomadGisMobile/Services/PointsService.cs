using NomadGisMobile.Models;

namespace NomadGisMobile.Services;

public class PointsService
{
    private readonly ApiClient _api;

    public PointsService(ApiClient api)
    {
        _api = api;
    }

    public async Task<List<MapPointDto>?> GetPointsAsync()
    {
        return await _api.GetAsync<List<MapPointDto>>("/api/v1/points");
    }
}
