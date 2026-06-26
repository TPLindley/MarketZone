using mzWheeler.Models;

namespace mzWheeler.Services;

/// <summary>
/// Service for accessing device GPS/location data
/// </summary>
public class LocationService
{
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isMonitoring;

    public event EventHandler<LocationData>? LocationUpdated;
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Get current location once
    /// </summary>
    public async Task<LocationData?> GetCurrentLocationAsync()
    {
        try
        {
            StatusChanged?.Invoke(this, "Getting location...");

            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                var locationData = new LocationData
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Altitude = location.Altitude ?? 0,
                    Speed = location.Speed ?? 0,
                    Course = location.Course ?? 0,
                    Accuracy = location.Accuracy ?? 0,
                    Timestamp = location.Timestamp.DateTime
                };

                StatusChanged?.Invoke(this, "Location acquired");
                return locationData;
            }

            StatusChanged?.Invoke(this, "Location not available");
            return null;
        }
        catch (FeatureNotSupportedException)
        {
            StatusChanged?.Invoke(this, "GPS not supported");
            return null;
        }
        catch (PermissionException)
        {
            StatusChanged?.Invoke(this, "Location permission denied");
            return null;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Start monitoring location continuously
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
            return;

        try
        {
            _isMonitoring = true;
            _cancelTokenSource = new CancellationTokenSource();

            StatusChanged?.Invoke(this, "Monitoring location...");

            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(10)
            };

            while (!_cancelTokenSource.Token.IsCancellationRequested)
            {
                var location = await Geolocation.GetLocationAsync(request, _cancelTokenSource.Token);

                if (location != null)
                {
                    var locationData = new LocationData
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Altitude = location.Altitude ?? 0,
                        Speed = location.Speed ?? 0,
                        Course = location.Course ?? 0,
                        Accuracy = location.Accuracy ?? 0,
                        Timestamp = location.Timestamp.DateTime
                    };

                    LocationUpdated?.Invoke(this, locationData);
                }

                await Task.Delay(1000, _cancelTokenSource.Token); // Update every second
            }
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke(this, "Monitoring stopped");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
        }
        finally
        {
            _isMonitoring = false;
        }
    }

    /// <summary>
    /// Stop monitoring location
    /// </summary>
    public void StopMonitoring()
    {
        if (_isMonitoring && _cancelTokenSource != null)
        {
            _cancelTokenSource.Cancel();
            _isMonitoring = false;
            StatusChanged?.Invoke(this, "Monitoring stopped");
        }
    }

    /// <summary>
    /// Check if location permission is granted
    /// </summary>
    public async Task<bool> CheckPermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        return status == PermissionStatus.Granted;
    }
}
