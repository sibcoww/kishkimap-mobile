using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.Maui.Storage;
using System;

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
                BaseAddress = new Uri("https://nomad-gis-api-7d6a.onrender.com"),
                Timeout = TimeSpan.FromSeconds(15) // prevent indefinite hangs
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
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("HttpClient is null");

                var json = JsonSerializer.Serialize(body, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"POST {url} returned {(int)response.StatusCode} {response.ReasonPhrase}");
                    AppendLog($"POST {url} returned {(int)response.StatusCode} {response.ReasonPhrase}");
                    return default;
                }

                var responseText = response.Content == null ? null : await response.Content.ReadAsStringAsync();
                if (responseText == null)
                {
                    Debug.WriteLine($"POST {url} returned empty content");
                    AppendLog($"POST {url} returned empty content");
                    return default;
                }

                AppendLog($"POST {url} success: {responseText.Substring(0, Math.Min(500, responseText.Length))}");
                return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in PostAsync to {url}: {ex}");
                AppendLog($"Exception in PostAsync to {url}: {ex}");
                throw new Exception($"PostAsync failed for {url}: {ex.Message}", ex);
            }
        }

        // GET-запрос без тела
        public async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("HttpClient is null");

                AppendLog($"GET {url} starting");
                Debug.WriteLine($"GET {url} starting");

                var response = await _httpClient.GetAsync(url);
                var status = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    var respText = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"GET {url} returned {status} {response.ReasonPhrase}");
                    AppendLog($"GET {url} returned {status} {response.ReasonPhrase}");
                    if (!string.IsNullOrEmpty(respText))
                        AppendLog($"GET {url} body: {respText.Substring(0, Math.Min(1000, respText.Length))}");

                    return default;
                }

                var responseText = response.Content == null ? null : await response.Content.ReadAsStringAsync();
                if (responseText == null)
                {
                    Debug.WriteLine($"GET {url} returned empty content");
                    AppendLog($"GET {url} returned empty content");
                    return default;
                }

                AppendLog($"GET {url} success: {responseText.Substring(0, Math.Min(1000, responseText.Length))}");
                return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetAsync to {url}: {ex}");
                AppendLog($"Exception in GetAsync to {url}: {ex}");
                throw new Exception($"GetAsync failed for {url}: {ex.Message}", ex);
            }
        }

        private void AppendLog(string text)
        {
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "profile_debug.log");
                var line = DateTime.UtcNow.ToString("o") + " " + text + Environment.NewLine;
                File.AppendAllText(path, line);
            }
            catch { }
        }
    }
}
