using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

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
                Timeout = TimeSpan.FromSeconds(15)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public void SetBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T?> PostAsync<T>(string url, object body)
        {
            try
            {
                var json = JsonSerializer.Serialize(body, _jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content)
                                                .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var respText = response.Content == null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    AppendLog($"POST {url} returned {(int)response.StatusCode} {response.ReasonPhrase}");
                    if (!string.IsNullOrEmpty(respText))
                        AppendLog($"POST {url} body: {Trim(respText, 1000)}");

                    return default;
                }

                var responseText = response.Content == null
                    ? null
                    : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(responseText))
                {
                    AppendLog($"POST {url} returned empty content");
                    return default;
                }

                AppendLog($"POST {url} success: {Trim(responseText, 500)}");
                return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
            }
            catch (Exception ex)
            {
                AppendLog($"Exception in PostAsync to {url}: {ex}");
                throw new Exception($"PostAsync failed for {url}: {ex.Message}", ex);
            }
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                AppendLog($"GET {url} starting");

                var response = await _httpClient.GetAsync(url)
                                                .ConfigureAwait(false);

                var status = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    var respText = response.Content == null
                        ? string.Empty
                        : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    AppendLog($"GET {url} returned {status} {response.ReasonPhrase}");
                    if (!string.IsNullOrEmpty(respText))
                        AppendLog($"GET {url} body: {Trim(respText, 1000)}");

                    return default;
                }

                var responseText = response.Content == null
                    ? null
                    : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(responseText))
                {
                    AppendLog($"GET {url} returned empty content");
                    return default;
                }

                AppendLog($"GET {url} success: {Trim(responseText, 1000)}");
                return JsonSerializer.Deserialize<T>(responseText, _jsonOptions);
            }
            catch (Exception ex)
            {
                AppendLog($"Exception in GetAsync to {url}: {ex}");
                throw new Exception($"GetAsync failed for {url}: {ex.Message}", ex);
            }
        }

        private static string Trim(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLen ? text : text.Substring(0, maxLen);
        }

        private void AppendLog(string text)
        {
            // Лог пишем с бэкграунд-потока, чтобы не блокировать UI
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        var path = Path.Combine(FileSystem.AppDataDirectory, "profile_debug.log");
                        var line = DateTime.UtcNow.ToString("o") + " " + text + Environment.NewLine;
                        File.AppendAllText(path, line);
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
