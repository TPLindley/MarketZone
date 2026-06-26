using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace mzConfigure.Services;

public partial class NetworkDiagnosticsService : INetworkDiagnosticsService
{
#if !ANDROID
    public async Task<PingResult> PingAsync(string hostOrIp, int timeoutMs = 5000)
    {
        var result = new PingResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
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
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Message = $"Ping failed: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    public async Task<string?> GetLocalIpAddressAsync()
    {
        try
        {
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
            // Try to ping a reliable public DNS server
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);
            return reply.Status == IPStatus.Success;
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
            report.LocalIpAddress = await GetLocalIpAddressAsync();
            report.IsConnected = !string.IsNullOrEmpty(report.LocalIpAddress);

            if (!report.IsConnected)
            {
                report.Issues.Add("Device does not have a local IP address");
                report.Summary = "Not connected to any network";
                return report;
            }

            report.NetworkType = "Network";
            report.InternetAvailable = await IsInternetAvailableAsync();

            if (!report.InternetAvailable)
            {
                report.Issues.Add("⚠ No internet connectivity detected (local network may still work)");
            }

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
        catch (System.Net.Http.HttpRequestException ex)
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
#endif
}
