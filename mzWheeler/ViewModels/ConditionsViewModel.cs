using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using mzWheeler.Models;
using mzWheeler.Services;

namespace mzWheeler.ViewModels;

/// <summary>
/// ViewModel for the Conditions page showing GPS and tilt data
/// </summary>
public class ConditionsViewModel : INotifyPropertyChanged
{
    private readonly LocationService _locationService;
    private readonly TiltService _tiltService;
    private readonly MockDataService _mockDataService;
    private LocationData _locationData = new();
    private TiltData _tiltData = new();
    private string _locationStatus = "Not monitoring";
    private string _tiltStatus = "Not monitoring";
    private bool _isMonitoring;
    private bool _useMockData;
    private CancellationTokenSource? _mockDataCts;

    public ConditionsViewModel()
    {
        _locationService = new LocationService();
        _tiltService = new TiltService();
        _mockDataService = new MockDataService();

        _locationService.LocationUpdated += OnLocationUpdated;
        _locationService.StatusChanged += OnLocationStatusChanged;
        _tiltService.TiltUpdated += OnTiltUpdated;
        _tiltService.StatusChanged += OnTiltStatusChanged;

        StartMonitoringCommand = new Command(async () => await StartMonitoring());
        StopMonitoringCommand = new Command(StopMonitoring);

        // Enable mock data on Windows or when sensors are not available
        _useMockData = DeviceInfo.Platform == DevicePlatform.WinUI || 
                       DeviceInfo.Platform == DevicePlatform.macOS;

        // Auto-start mock data on Windows
        if (_useMockData)
        {
            LocationStatus = "Using simulated GPS (Windows/Mac mode)";
            TiltStatus = "Using simulated tilt (Windows/Mac mode)";
            _ = StartMockDataAsync();
        }
    }

    public LocationData LocationData
    {
        get => _locationData;
        set
        {
            _locationData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Latitude));
            OnPropertyChanged(nameof(Longitude));
            OnPropertyChanged(nameof(Altitude));
            OnPropertyChanged(nameof(Accuracy));
            OnPropertyChanged(nameof(GpsSpeed));
            OnPropertyChanged(nameof(Course));
            OnPropertyChanged(nameof(CourseDirection));
        }
    }

    public TiltData TiltData
    {
        get => _tiltData;
        set
        {
            _tiltData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Pitch));
            OnPropertyChanged(nameof(Roll));
            OnPropertyChanged(nameof(Yaw));
        }
    }

    // Individual properties for easier binding
    public double Latitude => _locationData.Latitude;
    public double Longitude => _locationData.Longitude;
    public double Altitude => _locationData.Altitude;
    public double Accuracy => _locationData.Accuracy;
    public double GpsSpeed => _locationData.Speed * 3.6; // Convert m/s to km/h
    public double Course => _locationData.Course;

    public string CourseDirection
    {
        get
        {
            var course = _locationData.Course;
            if (course >= 337.5 || course < 22.5) return "N";
            if (course >= 22.5 && course < 67.5) return "NE";
            if (course >= 67.5 && course < 112.5) return "E";
            if (course >= 112.5 && course < 157.5) return "SE";
            if (course >= 157.5 && course < 202.5) return "S";
            if (course >= 202.5 && course < 247.5) return "SW";
            if (course >= 247.5 && course < 292.5) return "W";
            if (course >= 292.5 && course < 337.5) return "NW";
            return "-";
        }
    }

    public double Pitch => _tiltData.Pitch;
    public double Roll => _tiltData.Roll;
    public double Yaw => _tiltData.Yaw;

    public string MonitoringStatus => IsMonitoring ? "Monitoring active" : "Not monitoring";

    public string LocationStatus
    {
        get => _locationStatus;
        set
        {
            _locationStatus = value;
            OnPropertyChanged();
        }
    }

    public string TiltStatus
    {
        get => _tiltStatus;
        set
        {
            _tiltStatus = value;
            OnPropertyChanged();
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set
        {
            _isMonitoring = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MonitoringStatus));
        }
    }

    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }

    private async Task StartMonitoring()
    {
        // Use mock data on Windows/Mac
        if (_useMockData)
        {
            _ = StartMockDataAsync();
            return;
        }

        // Check location permissions
        var hasPermission = await _locationService.CheckPermissionsAsync();
        if (!hasPermission)
        {
            LocationStatus = "Location permission denied. Try 'Use Mock Data' button.";
            return;
        }

        IsMonitoring = true;

        // Start location monitoring
        _ = _locationService.StartMonitoringAsync();

        // Start tilt monitoring
        try
        {
            _tiltService.StartMonitoring();
        }
        catch (FeatureNotSupportedException)
        {
            TiltStatus = "Tilt sensors not available on this device";
        }
    }

    private void StopMonitoring()
    {
        if (_mockDataCts != null)
        {
            _mockDataCts.Cancel();
            _mockDataCts.Dispose();
            _mockDataCts = null;
        }

        _locationService.StopMonitoring();
        _tiltService.StopMonitoring();
        IsMonitoring = false;
    }

    private async Task StartMockDataAsync()
    {
        IsMonitoring = true;
        _mockDataCts = new CancellationTokenSource();
        LocationStatus = "Simulated GPS active";
        TiltStatus = "Simulated tilt active";

        try
        {
            while (!_mockDataCts.Token.IsCancellationRequested)
            {
                var mockLocation = _mockDataService.GenerateMockLocationData();
                var mockTilt = _mockDataService.GenerateMockTiltData();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LocationData = mockLocation;
                    TiltData = mockTilt;
                });

                await Task.Delay(1000, _mockDataCts.Token); // Update every second
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
    }

    private void OnLocationUpdated(object? sender, LocationData data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LocationData = data;
        });
    }

    private void OnLocationStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LocationStatus = status;
        });
    }

    private void OnTiltUpdated(object? sender, TiltData data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TiltData = data;
        });
    }

    private void OnTiltStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TiltStatus = status;
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
