using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using mzConfigure.Models;

namespace mzConfigure.Services;

public class SpecialsApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://10.42.0.1:8765";
    private const int MaxRetries = 2;
    private const int RetryDelayMs = 500;

    public SpecialsApiService()
    {
        var handler = new HttpClientHandler
        {
            // Disable connection pooling to avoid stale connections
            MaxConnectionsPerServer = 1
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15) // Increased timeout
        };
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value;
    }

    /// <summary>
    /// Send a lightweight GET request to wake up the server if it's idle
    /// </summary>
    private async Task WakeUpServerAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var wakeUpResponse = await _httpClient.GetAsync($"{_baseUrl}/specials", cts.Token);
            // Don't care about the response, just wake up the connection
        }
        catch
        {
            // If wake-up fails, ignore - the actual request might still work
        }
    }

    /// <summary>
    /// GET /specials - Retrieve current display list
    /// </summary>
    public async Task<List<Special>> GetSpecialsAsync()
    {
        Exception lastException = null;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // Wake up server on first attempt
                if (attempt == 0)
                {
                    await WakeUpServerAsync();
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/specials");
                response.EnsureSuccessStatusCode();

                var specials = await response.Content.ReadFromJsonAsync<List<Special>>();
                return specials ?? new List<Special>();
            }
            catch (HttpRequestException ex)
            {
                lastException = new Exception($"Network error retrieving specials from {_baseUrl}: {ex.Message}. Check network connection.", ex);

                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(1000);
                    continue;
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = new Exception($"Request timeout retrieving specials from {_baseUrl}. Server may be slow or unreachable.", ex);

                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(1000);
                    continue;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve specials: {ex.Message}", ex);
            }
        }

        throw lastException ?? new Exception("Failed to retrieve specials after multiple attempts.");
    }

    /// <summary>
    /// DELETE /specials - Clear/reset the display
    /// </summary>
    public async Task ClearSpecialsAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/specials");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error clearing specials from {_baseUrl}: {ex.Message}. Check network connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request timeout clearing specials from {_baseUrl}. Server may be slow or unreachable.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to clear specials: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// POST /specials - Update the display with a new list
    /// </summary>
    public async Task<int> UpdateSpecialsAsync(List<Special> specials)
    {
        Exception lastException = null;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // Wake up server on first attempt
                if (attempt == 0)
                {
                    await WakeUpServerAsync();
                }

                var json = JsonSerializer.Serialize(specials);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/specials", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonSerializer.Deserialize<UpdateResponse>(responseBody, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    // If deserialization succeeded and we got a count, use it
                    if (result != null && result.Count > 0)
                    {
                        return result.Count;
                    }
                }
                catch
                {
                    // If deserialization fails, fall through to return specials count
                }

                // Fallback: if the API doesn't return a proper count, use the count we sent
                return specials.Count;
            }
            catch (HttpRequestException ex)
            {
                lastException = new Exception($"Network error updating specials to {_baseUrl}: {ex.Message}. Check network connection.", ex);

                // Retry on network errors with longer delay
                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(1000); // Increased delay to give server more time
                    continue;
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = new Exception($"Request timeout updating specials to {_baseUrl}. Server may be slow or unreachable.", ex);

                // Retry on timeout with longer delay
                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(1000); // Increased delay to give server more time
                    continue;
                }
            }
            catch (Exception ex)
            {
                // Don't retry on other exceptions
                throw new Exception($"Failed to update specials: {ex.Message}", ex);
            }
        }

        // If we exhausted all retries, throw the last exception
        throw lastException ?? new Exception("Failed to update specials after multiple attempts.");
    }

    /// <summary>
    /// Test connection to the Raspberry Pi
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/specials", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error connecting to {_baseUrl}: {ex.Message}. Check if the Pi is powered on and you're on the correct network.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Connection timeout to {_baseUrl}. The Pi may be unreachable or the address is incorrect.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unexpected error connecting to {_baseUrl}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// GET /header - Retrieve current header text and color
    /// </summary>
    public async Task<HeaderInfo> GetHeaderAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/header");
            response.EnsureSuccessStatusCode();

            var header = await response.Content.ReadFromJsonAsync<HeaderInfo>();
            return header ?? new HeaderInfo();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error retrieving header from {_baseUrl}: {ex.Message}. Check network connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request timeout retrieving header from {_baseUrl}. Server may be slow or unreachable.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve header: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// POST /header - Set header text and/or color
    /// </summary>
    public async Task SetHeaderAsync(string text, string color)
    {
        try
        {
            var payload = new { text, color };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/header", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error setting header to {_baseUrl}: {ex.Message}. Check network connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request timeout setting header to {_baseUrl}. Server may be slow or unreachable.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to set header: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// GET /orientation - Retrieve current display orientation preference
    /// </summary>
    public async Task<string> GetOrientationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/orientation");
            response.EnsureSuccessStatusCode();

            var orientation = await response.Content.ReadFromJsonAsync<OrientationInfo>();
            return NormalizeOrientation(orientation?.Orientation);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error retrieving orientation from {_baseUrl}: {ex.Message}. Check network connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request timeout retrieving orientation from {_baseUrl}. Server may be slow or unreachable.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve orientation: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// POST /orientation - Set display orientation preference
    /// </summary>
    public async Task SetOrientationAsync(string orientation)
    {
        try
        {
            var payload = new OrientationInfo { Orientation = NormalizeOrientation(orientation) };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/orientation", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error setting orientation to {_baseUrl}: {ex.Message}. Check network connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request timeout setting orientation to {_baseUrl}. Server may be slow or unreachable.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to set orientation: {ex.Message}", ex);
        }
    }

    private static string NormalizeOrientation(string? orientation)
    {
        return string.Equals(orientation, "portrait", StringComparison.OrdinalIgnoreCase)
            ? "portrait"
            : "landscape";
    }

    public class HeaderInfo
    {
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class OrientationInfo
    {
        [JsonPropertyName("orientation")]
        public string Orientation { get; set; } = "landscape";
    }

    private class UpdateResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
