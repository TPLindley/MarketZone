using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using mzConfigure.Models;

namespace mzConfigure.Services;

public class SpecialsApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://raspberrypi.local:8765";

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
            var json = JsonSerializer.Serialize(specials);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/specials", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UpdateResponse>(responseBody);

            return result?.Count ?? 0;
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/specials");
            return response.IsSuccessStatusCode;
        }
        catch
        {
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/header");
            response.EnsureSuccessStatusCode();

            var header = await response.Content.ReadFromJsonAsync<HeaderInfo>();
            return header ?? new HeaderInfo();
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
        catch (Exception ex)
        {
            throw new Exception($"Failed to set header: {ex.Message}", ex);
        }
    }

    public class HeaderInfo
    {
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    private class UpdateResponse
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
