using System.Threading.Tasks;
using NomadGisMobile.Models;

namespace NomadGisMobile.Services
{
    public class AuthService
    {
        private readonly ApiClient _apiClient;

        public AuthService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            var request = new LoginRequest
            {
                Identifier = email,      // email или username
                Password = password,
                DeviceId = "mobile-app"  // можно любое строковое, но обязательно
            };

            return await _apiClient.PostAsync<LoginResponse>(
                "/api/v1/auth/login",
                request);
        }

    }
}
