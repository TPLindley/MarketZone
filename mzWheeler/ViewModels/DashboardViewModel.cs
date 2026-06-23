using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using mzWheeler.Models;
using mzWheeler.Services;

namespace mzWheeler.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page showing vehicle data
/// </summary>
public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly ObdBluetoothService _obdService;
    private readonly MockDataService _mockDataService;
    private VehicleData _vehicleData = new();
    private string _connectionStatus = "Not connected";
    private bool _isConnected;
    private bool _useMockData;
    private CancellationTokenSource? _mockDataCts;

    public DashboardViewModel()
    {
        _obdService = new ObdBluetoothService();
        _mockDataService = new MockDataService();

        _obdService.DataReceived += OnVehicleDataReceived;
        _obdService.ConnectionStatusChanged += OnConnectionStatusChanged;

        // Enable mock data on Windows or when BLE is not available
        _useMockData = DeviceInfo.Platform == DevicePlatform.WinUI || 
                       DeviceInfo.Platform == DevicePlatform.macOS;

        ConnectCommand = new Command(async () => await ConnectToObd());
        DisconnectCommand = new Command(async () => await Disconnect());
        ToggleMockDataCommand = new Command(() => ToggleMockData());

        // Auto-start mock data on Windows
        if (_useMockData)
        {
            ConnectionStatus = "Using simulated data (Windows/Mac mode)";
            _ = StartMockDataAsync();
        }
    }

    public VehicleData VehicleData
    {
        get => _vehicleData;
        set
        {
            _vehicleData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Speed));
            OnPropertyChanged(nameof(Rpm));
            OnPropertyChanged(nameof(EngineLoad));
            OnPropertyChanged(nameof(Throttle));
            OnPropertyChanged(nameof(CoolantTemp));
            OnPropertyChanged(nameof(FuelLevel));
            OnPropertyChanged(nameof(BatteryVoltage));
        }
    }

    // Individual properties for easier binding
    public double Speed => _vehicleData.Speed;
    public double Rpm => _vehicleData.Rpm;
    public double EngineLoad => _vehicleData.EngineLoad;
    public double Throttle => _vehicleData.Throttle;
    public double CoolantTemp => _vehicleData.CoolantTemp;
    public double FuelLevel => _vehicleData.FuelLevel;
    public double BatteryVoltage => _vehicleData.BatteryVoltage;

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;
            OnPropertyChanged();
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
        }
    }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ToggleMockDataCommand { get; }

    private async Task ConnectToObd()
    {
        // Stop mock data if running
        if (_useMockData)
        {
            StopMockData();
        }

        ConnectionStatus = "Scanning for devices...";

        var devices = await _obdService.ScanForDevicesAsync(TimeSpan.FromSeconds(10));

        if (devices.Any())
        {
            // Try to connect to the first OBD device found
            // In production, you'd want to let the user select from a list
            var obdDevice = devices.FirstOrDefault(d => d.Name?.Contains("OBD", StringComparison.OrdinalIgnoreCase) ?? false)
                          ?? devices.First();

            var connected = await _obdService.ConnectToDeviceAsync(obdDevice);

            if (connected)
            {
                IsConnected = true;
                _useMockData = false;
                // Start polling for data
                _ = _obdService.StartDataPollingAsync();
            }
        }
        else
        {
            ConnectionStatus = "No devices found. Tap 'Use Mock Data' to see simulated values.";
        }
    }

    private async Task Disconnect()
    {
        await _obdService.DisconnectAsync();
        IsConnected = false;
    }

    private void ToggleMockData()
    {
        if (_useMockData && _mockDataCts != null)
        {
            StopMockData();
            ConnectionStatus = "Mock data stopped";
        }
        else
        {
            _ = StartMockDataAsync();
        }
    }

    private async Task StartMockDataAsync()
    {
        _useMockData = true;
        IsConnected = true;
        _mockDataCts = new CancellationTokenSource();
        ConnectionStatus = "Simulated data active";

        try
        {
            while (!_mockDataCts.Token.IsCancellationRequested)
            {
                var mockData = _mockDataService.GenerateMockVehicleData();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    VehicleData = mockData;
                });

                await Task.Delay(500, _mockDataCts.Token); // Update every 500ms
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
    }

    private void StopMockData()
    {
        _mockDataCts?.Cancel();
        _mockDataCts?.Dispose();
        _mockDataCts = null;
        _useMockData = false;
        IsConnected = false;
    }

    private void OnVehicleDataReceived(object? sender, VehicleData data)
    {
        // Update on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            VehicleData = data;
        });
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionStatus = status;
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
