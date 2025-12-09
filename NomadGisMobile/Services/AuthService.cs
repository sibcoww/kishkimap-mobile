using NomadGisMobile.Models;

namespace NomadGisMobile.Services;

public class AuthService
{
    private readonly ApiClient _apiClient;

    public AuthService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // ---------- LOGIN ----------
    public async Task<LoginResponse?> LoginAsync(string identifier, string password)
    {
        var request = new LoginRequest
        {
            Identifier = identifier,
            Password = password,
            DeviceId = await GetOrCreateDeviceIdAsync()
        };

        return await _apiClient.PostAsync<LoginResponse>(
            "/api/v1/auth/login",
            request);
    }

    // ---------- REGISTER ----------
    public async Task<bool> RegisterAsync(string email, string username, string password)
    {
        var body = new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = password,
            DeviceId = await GetOrCreateDeviceIdAsync()
        };

        var result = await _apiClient.PostAsync<object>(
            "/api/v1/auth/register",
            body);

        return result != null;
    }

    // ---------- DEVICE ID ----------
    private async Task<string> GetOrCreateDeviceIdAsync()
    {
        string? id = await SecureStorage.GetAsync("device_id");

        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync("device_id", id);
        }

        return id;
    }
}
