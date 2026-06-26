namespace mzConfigure.Services;

public interface INetworkDiagnosticsService
{
    /// <summary>
    /// Pings a host to check basic network connectivity
    /// </summary>
    /// <param name="hostOrIp">Hostname or IP address to ping</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>Diagnostic result with success status and round-trip time</returns>
    Task<PingResult> PingAsync(string hostOrIp, int timeoutMs = 5000);

    /// <summary>
    /// Gets the device's local IP address on the current network
    /// </summary>
    /// <returns>Local IP address or null if not connected</returns>
    Task<string?> GetLocalIpAddressAsync();

    /// <summary>
    /// Checks if the device can reach the internet
    /// </summary>
    /// <returns>True if internet is reachable</returns>
    Task<bool> IsInternetAvailableAsync();

    /// <summary>
    /// Performs a comprehensive network diagnostic
    /// </summary>
    /// <param name="targetHost">Target host to test connectivity to</param>
    /// <returns>Detailed diagnostic report</returns>
    Task<NetworkDiagnosticReport> RunDiagnosticsAsync(string targetHost);
}

public class PingResult
{
    public bool Success { get; set; }
    public long RoundTripTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}

public class NetworkDiagnosticReport
{
    public bool IsConnected { get; set; }
    public string? LocalIpAddress { get; set; }
    public string? NetworkType { get; set; }
    public PingResult? PingResult { get; set; }
    public bool InternetAvailable { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}
