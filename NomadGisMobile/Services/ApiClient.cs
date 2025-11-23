using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NomadGisMobile.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://nomad-gis-api-7d6a.onrender.com")
            };

            // Настройки JSON — camelCase + нечувствительность регистра
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public void SetBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // POST-запрос с телом
        public async Task<T?> PostAsync<T>(string url, object body)
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return default;

            var responseText = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
        }

        // GET-запрос без тела
        public async Task<T?> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return default;

            var responseText = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
        }
    }
}
