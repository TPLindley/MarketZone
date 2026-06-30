#if IOS || MACCATALYST
using Foundation;
using NetworkExtension;

namespace mzConfigure.Services;

public partial class WiFiService : IWiFiService
{
    public async Task<bool> ConnectToNetworkAsync(string ssid, string password = "")
    {
        try
        {
            // iOS requires user interaction to connect to WiFi
            // We can only suggest/configure the network
            var configuration = string.IsNullOrEmpty(password)
                ? new NEHotspotConfiguration(ssid)  // Open network
                : new NEHotspotConfiguration(ssid, password, false);  // WPA/WPA2

            configuration.JoinOnce = false; // Persist the configuration

            var tcs = new TaskCompletionSource<bool>();

            NEHotspotConfigurationManager.SharedManager.ApplyConfiguration(configuration, (error) =>
            {
                if (error != null)
                {
                    // Error codes:
                    // NEHotspotConfigurationError.AlreadyAssociated = already connected
                    // NEHotspotConfigurationError.UserDenied = user cancelled
                    if (error.Code == (long)NEHotspotConfigurationError.AlreadyAssociated)
                    {
                        tcs.SetResult(true);
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.DisplayAlert("Connection Failed",
                                $"Failed to configure WiFi: {error.LocalizedDescription}", "OK");
                        });
                        tcs.SetResult(false);
                    }
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.DisplayAlert("WiFi Configured",
                            $"Successfully configured WiFi network '{ssid}'.", "OK");
                    });
                    tcs.SetResult(true);
                }
            });

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"WiFi configuration failed: {ex.Message}", "OK");
            return false;
        }
    }

    public async Task<string?> GetCurrentSsidAsync()
    {
        try
        {
            // iOS has restricted access to WiFi information
            // This may require specific entitlements and may not work in all cases
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsWiFiEnabledAsync()
    {
        // iOS doesn't provide a direct API to check if WiFi is enabled
        // We can only check if we're connected to a WiFi network
        var ssid = await GetCurrentSsidAsync();
        return !string.IsNullOrEmpty(ssid);
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
#endif
