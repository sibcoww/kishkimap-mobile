using System.Net.Http;
using System.Net.Http.Headers;
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

        public async Task<ProfileMeDto?> GetMeAsync()
        {
            return await _apiClient.GetAsync<ProfileMeDto>("/api/v1/profile/me");
        }

        /// <summary>
        /// Обновляем профиль через PUT /api/v1/profile/me (только текстовые поля).
        /// Любое из полей можно передать null – тогда оно не меняется.
        /// </summary>
        public async Task UpdateProfileAsync(string? username, string? currentPassword, string? newPassword)
        {
            using var content = new MultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(username))
                content.Add(new StringContent(username), "Username");

            if (!string.IsNullOrWhiteSpace(currentPassword))
                content.Add(new StringContent(currentPassword), "CurrentPassword");

            if (!string.IsNullOrWhiteSpace(newPassword))
                content.Add(new StringContent(newPassword), "NewPassword");

            await _apiClient.PutMultipartAsync("/api/v1/profile/me", content);
        }
    }
}
