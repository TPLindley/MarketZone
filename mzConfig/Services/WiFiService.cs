namespace mzConfigure.Services;

// Fallback implementation for platforms without full WiFi support implementation
#if !ANDROID && !IOS
public partial class WiFiService : IWiFiService
{
    public async Task<bool> ConnectToNetworkAsync(string ssid, string password = "")
    {
        await Shell.Current.DisplayAlert("Not Supported",
            "WiFi management is not fully supported on this platform.", "OK");
        return false;
    }

    public async Task<string?> GetCurrentSsidAsync()
    {
        return null;
    }

    public async Task<bool> IsWiFiEnabledAsync()
    {
        return false;
    }

    public async Task<string?> GetLocalIpAddressAsync()
    {
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipv4Address = addresses.FirstOrDefault(a =>
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !System.Net.IPAddress.IsLoopback(a));

            return ipv4Address?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
#else
// Platform-specific implementations are in:
// - Platforms/Android/WiFiService.cs
// - Platforms/iOS/WiFiService.cs (also used for MacCatalyst)
public partial class WiFiService : IWiFiService
{
}
#endif

