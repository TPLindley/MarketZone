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

    public SpecialsApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value;
    }

    /// <summary>
    /// GET /specials - Retrieve current display list
    /// </summary>
    public async Task<List<Special>> GetSpecialsAsync()
    {
        try
        {
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}/specials");
            response.EnsureSuccessStatusCode();
            
            var specials = await response.Content.ReadFromJsonAsync<List<Special>>();
            return specials ?? new List<Special>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve specials: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// DELETE /specials - Clear/reset the display
    /// </summary>
    public async Task ClearSpecialsAsync()
    {
        try
        {
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/specials");
            response.EnsureSuccessStatusCode();
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
        try
        {
            ApiAuthService.ApplyTokenHeader(_httpClient);
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
        catch (Exception ex)
        {
            throw new Exception($"Failed to update specials: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test connection to the Raspberry Pi
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            Log.Debug($"TestConnection: GET {_baseUrl}/specials");
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}/specials");
            var success = response.IsSuccessStatusCode;
            Log.Debug($"TestConnection: Status={response.StatusCode}, Success={success}");
            return success;
        }
        catch (Exception ex)
        {
            Log.Warning($"TestConnection: Failed - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// GET /header - Retrieve current header text and color
    /// </summary>
    public async Task<HeaderInfo> GetHeaderAsync()
    {
        try
        {
            Log.Debug($"API: Calling GET {_baseUrl}/header");
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}/header");
            Log.Debug($"API: Header endpoint returned status {(int)response.StatusCode} {response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var header = await response.Content.ReadFromJsonAsync<HeaderInfo>();
            Log.Debug($"API: Header response - Text: '{header?.Text}', Color: '{header?.Color}'");
            return header ?? new HeaderInfo();
        }
        catch (Exception ex)
        {
            Log.Error($"API: GetHeaderAsync failed - {ex.Message}");
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
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var payload = new { text, color };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/header", content);
            response.EnsureSuccessStatusCode();
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
            Log.Debug($"API: Calling GET {_baseUrl}/orientation");
            ApiAuthService.ApplyTokenHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}/orientation");
            Log.Debug($"API: Orientation endpoint returned status {(int)response.StatusCode} {response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var orientation = await response.Content.ReadFromJsonAsync<OrientationInfo>();
            var normalized = NormalizeOrientation(orientation?.Orientation);
            Log.Debug($"API: Orientation response - Raw: '{orientation?.Orientation}', Normalized: '{normalized}'");
            return normalized;
        }
        catch (Exception ex)
        {
            Log.Error($"API: GetOrientationAsync failed - {ex.Message}");
            throw new Exception($"Failed to retrieve orientation: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// POST /orientation - Set display orientation preference
    /// </summary>
    public async Task SetOrientationAsync(string orientation)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        var normalizedOrientation = NormalizeOrientation(orientation);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"{_baseUrl}/orientation";
                Log.Debug($"API: Calling POST {url} with orientation={normalizedOrientation} (Attempt {attempt}/{maxRetries})");

                ApiAuthService.ApplyTokenHeader(_httpClient);
                var payload = new OrientationInfo { Orientation = normalizedOrientation };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                Log.Debug($"API: Orientation endpoint returned status {(int)response.StatusCode} {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                Log.Info($"API: Orientation set successfully to '{normalizedOrientation}' on attempt {attempt}");
                return;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries && 
                (ex.Message.Contains("Connection reset") || 
                 ex.Message.Contains("connection was closed") ||
                 ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable))
            {
                Log.Warning($"API: SetOrientationAsync connection issue on attempt {attempt} - {ex.Message}, retrying after {retryDelayMs}ms...");
                await Task.Delay(retryDelayMs);
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries)
            {
                Log.Warning($"API: SetOrientationAsync timeout on attempt {attempt}, retrying after {retryDelayMs}ms...");
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"API: SetOrientationAsync failed on attempt {attempt}");
                throw new Exception($"Failed to set orientation: {ex.Message}", ex);
            }
        }

        // If we exhausted all retries
        throw new Exception($"Failed to set orientation after {maxRetries} attempts - connection keeps resetting");
    }

    private static string NormalizeOrientation(string? orientation)
    {
        return string.Equals(orientation, "portrait", StringComparison.OrdinalIgnoreCase)
            ? "portrait"
            : "landscape";
    }

    /// <summary>
    /// POST /blanking/trigger - Trigger screen animation test
    /// </summary>
    public async Task<bool> TriggerAnimationAsync()
    {
        const int maxRetries = 2;
        const int retryDelayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"{_baseUrl}/blanking/trigger";
                Log.Debug($"API: Calling POST {url} (Attempt {attempt}/{maxRetries})");
                ApiAuthService.ApplyTokenHeader(_httpClient);

                var response = await _httpClient.PostAsync(url, null);
                Log.Debug($"API: Animation trigger returned status {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Log.Warning($"API: Animation trigger failed (attempt {attempt}) - Status: {response.StatusCode}, Body: {content}");

                    // If 404 and we have retries left, wait and try again
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound && attempt < maxRetries)
                    {
                        Log.Info($"API: Retrying after {retryDelayMs}ms delay...");
                        await Task.Delay(retryDelayMs);
                        continue;
                    }
                }

                response.EnsureSuccessStatusCode();
                Log.Info($"API: Animation triggered successfully on attempt {attempt}");
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound && attempt < maxRetries)
            {
                Log.Warning($"API: TriggerAnimationAsync HTTP 404 on attempt {attempt}, retrying after {retryDelayMs}ms...");
                await Task.Delay(retryDelayMs);
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"API: TriggerAnimationAsync HTTP error - StatusCode: {ex.StatusCode}, Message: {ex.Message}");
                throw new Exception($"Failed to trigger animation: HTTP {ex.StatusCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "API: TriggerAnimationAsync failed");
                throw new Exception($"Failed to trigger animation: {ex.Message}", ex);
            }
        }

        // If we exhausted all retries
        throw new Exception($"Failed to trigger animation after {maxRetries} attempts");
    }

    /// <summary>
    /// GET /specials and /header to verify server state after an operation
    /// </summary>
    public async Task<string> GetServerStateAsync()
    {
        try
        {
            Log.Debug("GetServerState: Retrieving full server state");
            ApiAuthService.ApplyTokenHeader(_httpClient);

            var specialsResponse = await _httpClient.GetAsync($"{_baseUrl}/specials");
            var specialsStatus = specialsResponse.StatusCode;
            var specialsData = specialsResponse.IsSuccessStatusCode 
                ? await specialsResponse.Content.ReadAsStringAsync() 
                : "N/A";

            var headerResponse = await _httpClient.GetAsync($"{_baseUrl}/header");
            var headerStatus = headerResponse.StatusCode;
            var headerData = headerResponse.IsSuccessStatusCode 
                ? await headerResponse.Content.ReadAsStringAsync() 
                : "N/A";

            var orientationResponse = await _httpClient.GetAsync($"{_baseUrl}/orientation");
            var orientationStatus = orientationResponse.StatusCode;
            var orientationData = orientationResponse.IsSuccessStatusCode 
                ? await orientationResponse.Content.ReadAsStringAsync() 
                : "N/A";

            var state = $"SPECIALS [{specialsStatus}]: {specialsData}\n" +
                       $"HEADER [{headerStatus}]: {headerData}\n" +
                       $"ORIENTATION [{orientationStatus}]: {orientationData}";

            Log.Debug($"GetServerState:\n{state}");
            return state;
        }
        catch (Exception ex)
        {
            var error = $"Failed to retrieve server state: {ex.Message}";
            Log.Warning(error);
            return error;
        }
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

