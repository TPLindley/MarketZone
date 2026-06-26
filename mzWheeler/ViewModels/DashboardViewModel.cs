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

        DisconnectCommand = new Command(async () => await Disconnect());
        ToggleMockDataCommand = new Command(() => ToggleMockData());
        ForgetDeviceCommand = new Command(() => ForgetDevice());

        // Auto-start: mock data on Windows, OBD connection on iOS/Android
        if (_useMockData)
        {
            ConnectionStatus = "Using simulated data (Windows/Mac mode)";
            _ = StartMockDataAsync();
        }
        else
        {
            // Auto-connect to OBD on mobile platforms
            _ = AutoConnectToObd();
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

    public ICommand DisconnectCommand { get; }
    public ICommand ToggleMockDataCommand { get; }
    public ICommand ForgetDeviceCommand { get; }

    private async Task AutoConnectToObd()
    {
        ConnectionStatus = "Scanning for devices...";

        var devices = await _obdService.ScanForDevicesAsync(TimeSpan.FromSeconds(10));

        if (!devices.Any())
        {
            ConnectionStatus = "No devices found. Using mock data.";
            _useMockData = true;
            _ = StartMockDataAsync();
            return;
        }

        // Check if we have a previously connected device name
        var lastDeviceId = Preferences.Get("LastOBDDeviceId", string.Empty);
        var lastDeviceName = Preferences.Get("LastOBDDeviceName", string.Empty);

        Plugin.BLE.Abstractions.Contracts.IDevice? selectedDevice = null;

        // Try to auto-connect to the last device if it exists
        if (!string.IsNullOrEmpty(lastDeviceId))
        {
            selectedDevice = devices.FirstOrDefault(d => d.Id.ToString() == lastDeviceId);
            if (selectedDevice != null)
            {
                ConnectionStatus = $"Auto-connecting to {selectedDevice.Name}...";
            }
        }

        // If no previous device or it wasn't found, show device picker
        if (selectedDevice == null)
        {
            // Build device list for selection
            var deviceNames = devices.Select(d => $"{d.Name ?? "Unknown"} ({d.Id})").ToArray();

            var mainPage = Application.Current?.Windows[0]?.Page;
            if (mainPage == null)
            {
                ConnectionStatus = "Using mock data.";
                _useMockData = true;
                _ = StartMockDataAsync();
                return;
            }

            var selectedName = await mainPage.DisplayActionSheetAsync(
                "Select OBD-II Device",
                "Use Mock Data",
                null,
                deviceNames);

            if (selectedName == "Use Mock Data" || string.IsNullOrEmpty(selectedName))
            {
                ConnectionStatus = "Using mock data.";
                _useMockData = true;
                _ = StartMockDataAsync();
                return;
            }

            // Find the selected device
            var index = Array.IndexOf(deviceNames, selectedName);
            selectedDevice = devices[index];
        }

        // Connect to the selected device
        var connected = await _obdService.ConnectToDeviceAsync(selectedDevice);

        if (connected)
        {
            IsConnected = true;
            _useMockData = false;

            // Remember this device
            Preferences.Set("LastOBDDeviceId", selectedDevice.Id.ToString());
            Preferences.Set("LastOBDDeviceName", selectedDevice.Name ?? "Unknown");

            // Start polling for data
            _ = _obdService.StartDataPollingAsync();
        }
        else
        {
            ConnectionStatus = "Connection failed. Using mock data.";
            _useMockData = true;
            _ = StartMockDataAsync();
        }
    }

    private async Task Disconnect()
    {
        await _obdService.DisconnectAsync();
        IsConnected = false;
    }

    private void ForgetDevice()
    {
        Preferences.Remove("LastOBDDeviceId");
        Preferences.Remove("LastOBDDeviceName");
        ConnectionStatus = "Saved device forgotten. Next connection will show device picker.";
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
