#if ANDROID
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace mzConfigure.Services;

public partial class WiFiService : IWiFiService
{
    public async Task<bool> ConnectToNetworkAsync(string ssid, string password = "")
    {
        try
        {
            var wifiManager = (WifiManager?)Android.App.Application.Context.GetSystemService(Context.WifiService);
            if (wifiManager == null)
                return false;

            // Check if WiFi is enabled
            if (!wifiManager.IsWifiEnabled)
            {
                // Prompt user to enable WiFi
                await Shell.Current.DisplayAlert("WiFi Disabled", 
                    "Please enable WiFi to connect to the network.", "OK");
                return false;
            }

            // For Android 10 (API 29) and above, use NetworkSuggestion API
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                var suggestion = new WifiNetworkSuggestion.Builder()
                    .SetSsid(ssid);

                if (!string.IsNullOrEmpty(password))
                {
                    suggestion.SetWpa2Passphrase(password);
                }

                var suggestionsList = new List<WifiNetworkSuggestion> { suggestion.Build() };
                var status = wifiManager.AddNetworkSuggestions(suggestionsList);

                // Status code 0 = success
                if (status == 0)
                {
                    await Shell.Current.DisplayAlert("Network Suggested", 
                        $"WiFi network '{ssid}' has been suggested. Your device may automatically connect.", "OK");
                    return true;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Connection Failed", 
                        $"Failed to suggest network. Status code: {status}", "OK");
                    return false;
                }
            }
            else
            {
                // For older Android versions (deprecated but still functional on older devices)
                var wifiConfig = new WifiConfiguration
                {
                    Ssid = $"\"{ssid}\""
                };

                if (!string.IsNullOrEmpty(password))
                {
                    wifiConfig.PreSharedKey = $"\"{password}\"";
                }
                else
                {
                    wifiConfig.AllowedKeyManagement.Set((int)KeyManagementType.None);
                }

                var networkId = wifiManager.AddNetwork(wifiConfig);
                if (networkId != -1)
                {
                    wifiManager.Disconnect();
                    wifiManager.EnableNetwork(networkId, true);
                    wifiManager.Reconnect();
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"WiFi connection failed: {ex.Message}", "OK");
            return false;
        }
    }

    public async Task<string?> GetCurrentSsidAsync()
    {
        try
        {
            var wifiManager = (WifiManager?)Android.App.Application.Context.GetSystemService(Context.WifiService);
            if (wifiManager == null)
                return null;

            var connectionInfo = wifiManager.ConnectionInfo;
            if (connectionInfo != null)
            {
                var ssid = connectionInfo.SSID;
                // Remove quotes from SSID
                if (!string.IsNullOrEmpty(ssid))
                {
                    ssid = ssid.Trim('"');
                }
                return ssid;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsWiFiEnabledAsync()
    {
        try
        {
            var wifiManager = (WifiManager?)Android.App.Application.Context.GetSystemService(Context.WifiService);
            return wifiManager?.IsWifiEnabled ?? false;
        }
        catch
        {
            return false;
        }
    }
}
#endif
