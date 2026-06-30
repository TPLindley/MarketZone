namespace mzConfigure.Services;

public interface IWiFiService
{
    /// <summary>
    /// Attempts to connect to a WiFi network
    /// </summary>
    /// <param name="ssid">Network SSID</param>
    /// <param name="password">Network password (optional for open networks)</param>
    /// <returns>True if connection was successful or initiated</returns>
    Task<bool> ConnectToNetworkAsync(string ssid, string password = "");

    /// <summary>
    /// Gets the currently connected WiFi SSID
    /// </summary>
    /// <returns>SSID or null if not connected to WiFi</returns>
    Task<string?> GetCurrentSsidAsync();

    /// <summary>
    /// Checks if WiFi is currently enabled
    /// </summary>
    Task<bool> IsWiFiEnabledAsync();

    /// <summary>
    /// Gets the local IP address of the device
    /// </summary>
    /// <returns>IP address or null if not available</returns>
    Task<string?> GetLocalIpAddressAsync();
}
