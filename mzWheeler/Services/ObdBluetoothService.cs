using mzWheeler.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using System.Text;

namespace mzWheeler.Services;

/// <summary>
/// Service for communicating with OBD-II Bluetooth device
/// </summary>
public class ObdBluetoothService
{
    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private ICharacteristic? _readCharacteristic;
    private ICharacteristic? _writeCharacteristic;

    public bool IsConnected => _connectedDevice != null;
    public event EventHandler<VehicleData>? DataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public ObdBluetoothService()
    {
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
    }

    /// <summary>
    /// Scan for nearby Bluetooth devices
    /// </summary>
    public async Task<List<IDevice>> ScanForDevicesAsync(TimeSpan timeout)
    {
        var devices = new List<IDevice>();

        try
        {
            ConnectionStatusChanged?.Invoke(this, "Scanning for devices...");

            _adapter.ScanTimeout = (int)timeout.TotalMilliseconds;
            await _adapter.StartScanningForDevicesAsync();

            devices.AddRange(_adapter.DiscoveredDevices);
            ConnectionStatusChanged?.Invoke(this, $"Found {devices.Count} devices");
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Scan error: {ex.Message}");
        }

        return devices;
    }

    /// <summary>
    /// Connect to a specific OBD-II device
    /// </summary>
    public async Task<bool> ConnectToDeviceAsync(IDevice device)
    {
        try
        {
            ConnectionStatusChanged?.Invoke(this, $"Connecting to {device.Name}...");

            await _adapter.ConnectToDeviceAsync(device);
            _connectedDevice = device;

            // Discover services and characteristics
            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                foreach (var characteristic in characteristics)
                {
                    if (characteristic.CanWrite)
                        _writeCharacteristic = characteristic;
                    if (characteristic.CanRead || characteristic.CanUpdate)
                        _readCharacteristic = characteristic;
                }
            }

            if (_readCharacteristic != null)
            {
                _readCharacteristic.ValueUpdated += OnCharacteristicValueUpdated;
                await _readCharacteristic.StartUpdatesAsync();
            }

            ConnectionStatusChanged?.Invoke(this, "Connected successfully!");
            return true;
        }
        catch (DeviceConnectionException ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnect from the current device
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_connectedDevice != null)
        {
            try
            {
                if (_readCharacteristic != null)
                {
                    await _readCharacteristic.StopUpdatesAsync();
                    _readCharacteristic.ValueUpdated -= OnCharacteristicValueUpdated;
                }

                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;
                _readCharacteristic = null;
                _writeCharacteristic = null;

                ConnectionStatusChanged?.Invoke(this, "Disconnected");
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Disconnect error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Send an OBD-II command (PID request)
    /// </summary>
    public async Task<bool> SendCommandAsync(string command)
    {
        if (_writeCharacteristic == null || _connectedDevice == null)
            return false;

        try
        {
            var bytes = Encoding.ASCII.GetBytes(command + "\r");
            await _writeCharacteristic.WriteAsync(bytes);
            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Send error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Start continuous data polling from OBD-II
    /// </summary>
    public async Task StartDataPollingAsync()
    {
        if (!IsConnected) return;

        // Common OBD-II PIDs (Mode 01)
        var commands = new[]
        {
            "010D", // Vehicle speed
            "010C", // Engine RPM
            "0104", // Engine load
            "0111", // Throttle position
            "0105", // Coolant temperature
            "012F", // Fuel level
            "0142", // Control module voltage
        };

        while (IsConnected)
        {
            foreach (var cmd in commands)
            {
                await SendCommandAsync(cmd);
                await Task.Delay(100); // Delay between commands
            }
            await Task.Delay(500); // Delay between polling cycles
        }
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        // Device discovered during scan
    }

    private void OnDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        ConnectionStatusChanged?.Invoke(this, "Connection lost!");
        _connectedDevice = null;
    }

    private void OnCharacteristicValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        try
        {
            var data = Encoding.ASCII.GetString(e.Characteristic.Value);
            var vehicleData = ParseObdResponse(data);
            DataReceived?.Invoke(this, vehicleData);
        }
        catch (Exception ex)
        {
            // Handle parsing errors
            ConnectionStatusChanged?.Invoke(this, $"Parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse OBD-II response data into VehicleData
    /// </summary>
    private VehicleData ParseObdResponse(string response)
    {
        // Simplified OBD-II response parsing
        // Format: "41 0D 1E" = Mode 41 (response), PID 0D (speed), Value 1E (30 km/h)

        var vehicleData = new VehicleData();

        try
        {
            var parts = response.Trim().Split(' ');
            if (parts.Length < 3) return vehicleData;

            var mode = parts[0];
            var pid = parts[1];

            // Parse based on PID
            switch (pid)
            {
                case "0D": // Speed (km/h)
                    vehicleData.Speed = Convert.ToInt32(parts[2], 16);
                    break;
                case "0C": // RPM (1/4 RPM per bit)
                    vehicleData.Rpm = (Convert.ToInt32(parts[2], 16) * 256 + Convert.ToInt32(parts[3], 16)) / 4.0;
                    break;
                case "04": // Engine load (%)
                    vehicleData.EngineLoad = Convert.ToInt32(parts[2], 16) * 100.0 / 255.0;
                    break;
                case "11": // Throttle (%)
                    vehicleData.Throttle = Convert.ToInt32(parts[2], 16) * 100.0 / 255.0;
                    break;
                case "05": // Coolant temp (°C)
                    vehicleData.CoolantTemp = Convert.ToInt32(parts[2], 16) - 40;
                    break;
                case "2F": // Fuel level (%)
                    vehicleData.FuelLevel = Convert.ToInt32(parts[2], 16) * 100.0 / 255.0;
                    break;
                case "42": // Battery voltage (V)
                    vehicleData.BatteryVoltage = (Convert.ToInt32(parts[2], 16) * 256 + Convert.ToInt32(parts[3], 16)) / 1000.0;
                    break;
            }
        }
        catch
        {
            // Parsing error - return empty data
        }

        return vehicleData;
    }
}
