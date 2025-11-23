using NomadGisMobile.Models;

namespace NomadGisMobile.Services
{
    public class ProfileService
    {
        private readonly ApiClient _apiClient;

        public ProfileService(ApiClient api)
        {
            _apiClient = api;
        }

        public async Task<UserDto?> GetMeAsync()
        {
            return await _apiClient.GetAsync<UserDto>("/api/v1/profile/me");
        }
    }
}
