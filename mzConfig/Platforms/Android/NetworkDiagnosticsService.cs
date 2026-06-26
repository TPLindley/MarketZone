#if ANDROID
using Android.Content;
using Android.Net;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace mzConfigure.Services;

public partial class NetworkDiagnosticsService : INetworkDiagnosticsService
{
    public async Task<PingResult> PingAsync(string hostOrIp, int timeoutMs = 5000)
    {
        var result = new PingResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try using System.Net.NetworkInformation.Ping
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(hostOrIp, timeoutMs);
            stopwatch.Stop();

            if (reply.Status == IPStatus.Success)
            {
                result.Success = true;
                result.RoundTripTimeMs = reply.RoundtripTime;
                result.Message = $"Reply from {reply.Address}: time={reply.RoundtripTime}ms";
            }
            else
            {
                result.Success = false;
                result.Message = $"Ping failed: {reply.Status}";
                result.ErrorDetails = reply.Status.ToString();
            }
        }
        catch (PingException ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Message = "Ping not available, trying TCP connection test...";

            // Fallback: Try TCP connection as alternative
            try
            {
                var tcpResult = await TryTcpConnectionAsync(hostOrIp, 8765, timeoutMs);
                if (tcpResult)
                {
                    result.Success = true;
                    result.RoundTripTimeMs = stopwatch.ElapsedMilliseconds;
                    result.Message = $"TCP connection successful: time={stopwatch.ElapsedMilliseconds}ms (port 8765)";
                }
                else
                {
                    result.ErrorDetails = $"Ping not available and TCP connection failed. Original error: {ex.Message}";
                }
            }
            catch (Exception tcpEx)
            {
                result.ErrorDetails = $"Ping failed: {ex.Message}. TCP fallback also failed: {tcpEx.Message}";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Message = $"Error: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    private async Task<bool> TryTcpConnectionAsync(string host, int port, int timeoutMs)
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(timeoutMs);

        var completedTask = await Task.WhenAny(connectTask, timeoutTask);

        if (completedTask == connectTask && client.Connected)
        {
            return true;
        }

        return false;
    }

    public async Task<string?> GetLocalIpAddressAsync()
    {
        try
        {
            var connectivityManager = (ConnectivityManager?)Android.App.Application.Context
                .GetSystemService(Context.ConnectivityService);

            if (connectivityManager == null)
                return null;

            var activeNetwork = connectivityManager.ActiveNetwork;
            if (activeNetwork == null)
                return null;

            var linkProperties = connectivityManager.GetLinkProperties(activeNetwork);
            if (linkProperties?.LinkAddresses == null)
                return null;

            // Get the first IPv4 address
            foreach (var linkAddress in linkProperties.LinkAddresses)
            {
                var address = linkAddress?.Address?.HostAddress;
                if (address != null && !address.Contains(":") && !address.StartsWith("127."))
                {
                    return address;
                }
            }

            // Fallback method
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            var localIp = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork 
                    && !IPAddress.IsLoopback(ip));

            return localIp?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            var connectivityManager = (ConnectivityManager?)Android.App.Application.Context
                .GetSystemService(Context.ConnectivityService);

            if (connectivityManager == null)
                return false;

            var activeNetwork = connectivityManager.ActiveNetwork;
            if (activeNetwork == null)
                return false;

            var capabilities = connectivityManager.GetNetworkCapabilities(activeNetwork);
            if (capabilities == null)
                return false;

            return capabilities.HasCapability(NetCapability.Internet) 
                && capabilities.HasCapability(NetCapability.Validated);
        }
        catch
        {
            return false;
        }
    }

    public async Task<NetworkDiagnosticReport> RunDiagnosticsAsync(string targetHost)
    {
        var report = new NetworkDiagnosticReport();

        try
        {
            // Get local IP
            report.LocalIpAddress = await GetLocalIpAddressAsync();
            report.IsConnected = !string.IsNullOrEmpty(report.LocalIpAddress);

            if (!report.IsConnected)
            {
                report.Issues.Add("Device does not have a local IP address");
                report.Summary = "Not connected to any network";
                return report;
            }

            // Get network type
            report.NetworkType = await GetNetworkTypeAsync();

            // Check internet availability
            report.InternetAvailable = await IsInternetAvailableAsync();
            if (!report.InternetAvailable)
            {
                report.Issues.Add("⚠ No internet connectivity detected (local network may still work)");
            }

            // Ping target host
            report.PingResult = await PingAsync(targetHost, 5000);
            if (!report.PingResult.Success)
            {
                report.Issues.Add($"Cannot ping {targetHost}: {report.PingResult.Message}");
            }

            // Test actual HTTP endpoint on port 8765
            var httpTestResult = await TestHttpEndpointAsync(targetHost, 8765);
            if (!httpTestResult.Success)
            {
                report.Issues.Add($"⚠ HTTP service test failed on port 8765: {httpTestResult.Message}");
            }
            else
            {
                report.Issues.Add($"✓ HTTP service is responding on port 8765");
            }

            // Generate summary
            if (report.Issues.Count <= 2) // Only info messages
            {
                report.Summary = $"✓ Connected to {report.NetworkType}\n" +
                               $"✓ Local IP: {report.LocalIpAddress}\n" +
                               $"✓ Can reach {targetHost}\n" +
                               (report.PingResult != null ? $"✓ Ping time: {report.PingResult.RoundTripTimeMs}ms\n" : "") +
                               $"✓ HTTP service is responding (port 8765)";
            }
            else
            {
                report.Summary = $"Network Status:\n" +
                               $"Local IP: {report.LocalIpAddress ?? "None"}\n" +
                               $"Network: {report.NetworkType ?? "Unknown"}\n\n" +
                               string.Join("\n", report.Issues);
            }
        }
        catch (Exception ex)
        {
            report.Summary = $"Diagnostics failed: {ex.Message}";
            report.Issues.Add($"Error running diagnostics: {ex.Message}");
        }

        return report;
    }

    private async Task<(bool Success, string Message)> TestHttpEndpointAsync(string host, int port)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var url = $"http://{host}:{port}/specials";
            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"Server responded: {response.StatusCode}");
            }
            else
            {
                return (false, $"Server returned: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection refused or network error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, "Connection timeout - server not responding");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    private async Task<string?> GetNetworkTypeAsync()
    {
        try
        {
            var connectivityManager = (ConnectivityManager?)Android.App.Application.Context
                .GetSystemService(Context.ConnectivityService);

            if (connectivityManager == null)
                return "Unknown";

            var activeNetwork = connectivityManager.ActiveNetwork;
            if (activeNetwork == null)
                return "Not Connected";

            var capabilities = connectivityManager.GetNetworkCapabilities(activeNetwork);
            if (capabilities == null)
                return "Unknown";

            if (capabilities.HasTransport(TransportType.Wifi))
                return "WiFi";
            if (capabilities.HasTransport(TransportType.Cellular))
                return "Cellular";
            if (capabilities.HasTransport(TransportType.Ethernet))
                return "Ethernet";
            if (capabilities.HasTransport(TransportType.Bluetooth))
                return "Bluetooth";

            return "Other";
        }
        catch
        {
            return "Unknown";
        }
    }
}
#endif
