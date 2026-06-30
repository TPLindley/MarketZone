using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mzConfigure.Services;

/// <summary>
/// Monitors service health by periodically pinging the API to check if it's alive.
/// Displays service-reported uptime for battery runtime tracking.
/// </summary>
public class ServiceHealthMonitor : INotifyPropertyChanged
{
    private readonly SpecialsApiService _apiService;
    private PeriodicTimer? _healthCheckTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isMonitoring;
    private TimeSpan _checkInterval;
    private DateTime? _lastSuccessfulPing;
    private Task? _monitoringTask;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ServiceHealthMonitor(SpecialsApiService apiService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

        // Load interval from preferences (default 5 minutes)
        var intervalMinutes = Preferences.Get("HealthCheckIntervalMinutes", 5.0);
        _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    /// <summary>
    /// Gets whether the monitoring is currently active
    /// </summary>
    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (_isMonitoring != value)
            {
                _isMonitoring = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the service uptime in seconds from the last successful health check
    /// </summary>
    public int ServiceUptimeSeconds => _apiService.LastServiceUptimeSeconds;

    /// <summary>
    /// Gets or sets the check interval in minutes
    /// </summary>
    public double CheckIntervalMinutes
    {
        get => _checkInterval.TotalMinutes;
        set
        {
            if (Math.Abs(_checkInterval.TotalMinutes - value) > 0.001)
            {
                _checkInterval = TimeSpan.FromMinutes(value);
                Preferences.Set("HealthCheckIntervalMinutes", value);
                OnPropertyChanged();

                // Restart monitoring with new interval if currently active
                if (_isMonitoring)
                {
                    _ = RestartMonitoringAsync();
                }
            }
        }
    }

    /// <summary>
    /// Gets the service uptime as formatted text
    /// </summary>
    public string ServiceUptimeText
    {
        get
        {
            var totalSeconds = ServiceUptimeSeconds;
            if (totalSeconds == 0)
            {
                return "N/A";
            }

            var totalMinutes = totalSeconds / 60.0;
            if (totalMinutes < 60)
            {
                return $"{totalMinutes:F0} minutes";
            }
            else if (totalMinutes < 1440) // Less than 24 hours
            {
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;
                return $"{hours:F0}h {minutes:F0}m";
            }
            else
            {
                var days = totalMinutes / 1440;
                var hours = (totalMinutes % 1440) / 60;
                return $"{days:F0}d {hours:F0}h";
            }
        }
    }

    /// <summary>
    /// Gets the last successful ping timestamp
    /// </summary>
    public DateTime? LastSuccessfulPing
    {
        get => _lastSuccessfulPing;
        private set
        {
            if (_lastSuccessfulPing != value)
            {
                _lastSuccessfulPing = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Starts monitoring the service health
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
        {
            Log.Debug("ServiceHealthMonitor: Already monitoring");
            return;
        }

        Log.Info($"ServiceHealthMonitor: Starting health check monitoring (interval: {_checkInterval.TotalMinutes} minutes)");

        _cancellationTokenSource = new CancellationTokenSource();
        _healthCheckTimer = new PeriodicTimer(_checkInterval);
        IsMonitoring = true;

        // Perform initial health check immediately
        _ = Task.Run(async () => await PerformHealthCheckAsync());

        // Start the periodic monitoring loop
        _monitoringTask = Task.Run(async () => await MonitoringLoopAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Stops monitoring the service health
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            Log.Debug("ServiceHealthMonitor: Not currently monitoring");
            return;
        }

        Log.Info("ServiceHealthMonitor: Stopping health check monitoring");

        _cancellationTokenSource?.Cancel();
        _healthCheckTimer?.Dispose();

        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when canceling
            }
        }

        _healthCheckTimer = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitoringTask = null;
        IsMonitoring = false;
    }

    /// <summary>
    /// Resets the health check state (call when establishing initial connection)
    /// </summary>
    public void ResetCounter()
    {
        Log.Info("ServiceHealthMonitor: Resetting health check state");
        LastSuccessfulPing = null;
    }

    /// <summary>
    /// Notifies that a new connection session has been established
    /// This resets the state for fresh battery life tracking
    /// </summary>
    public void OnConnectionEstablished()
    {
        Log.Info("ServiceHealthMonitor: New connection established, resetting state");
        ResetCounter();
    }

    /// <summary>
    /// Restarts monitoring with updated interval
    /// </summary>
    private async Task RestartMonitoringAsync()
    {
        Log.Debug($"ServiceHealthMonitor: Restarting with new interval: {_checkInterval.TotalMinutes} minutes");
        await StopMonitoringAsync();
        await StartMonitoringAsync();
    }

    /// <summary>
    /// Main monitoring loop
    /// </summary>
    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _healthCheckTimer != null)
            {
                await _healthCheckTimer.WaitForNextTickAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await PerformHealthCheckAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Log.Debug("ServiceHealthMonitor: Monitoring loop cancelled");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ServiceHealthMonitor: Error in monitoring loop");
        }
    }

    /// <summary>
    /// Performs a single health check
    /// </summary>
    private async Task PerformHealthCheckAsync()
    {
        try
        {
            Log.Debug("ServiceHealthMonitor: Performing health check...");
            var isAlive = await _apiService.TestConnectionAsync();

            if (isAlive)
            {
                LastSuccessfulPing = DateTime.Now;

                // Trigger property changed to update UI with latest uptime from service
                OnPropertyChanged(nameof(ServiceUptimeSeconds));
                OnPropertyChanged(nameof(ServiceUptimeText));

                // Log service version and uptime on each health check
                var version = _apiService.LastServiceVersion;
                var uptimeSeconds = _apiService.LastServiceUptimeSeconds;

                Log.Info($"ServiceHealthMonitor: Health check PASSED - Service v{version}, Uptime: {uptimeSeconds}s ({ServiceUptimeText}), Last check: {LastSuccessfulPing:HH:mm:ss}");
            }
            // else: Silent fail - no log message for failed health checks
        }
        catch (Exception)
        {
            // Silent fail - no log message for exceptions during health check
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
