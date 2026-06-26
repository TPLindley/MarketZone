# mzWheeler - Vehicle Telemetry App

A .NET MAUI application for iPad that displays real-time vehicle telemetry data from OBD-II sensors via Bluetooth, along with GPS location and device tilt information.

## Features

### Dashboard Screen
Electronic dashboard displaying vehicle data from OBD-II sensor:
- **Speed** - Vehicle speed (km/h) with radial gauge
- **RPM** - Engine RPM with radial gauge
- **Engine Load** - Current engine load percentage with linear gauge
- **Throttle Position** - Throttle position percentage with linear gauge
- **Coolant Temperature** - Engine coolant temperature (°C)
- **Fuel Level** - Fuel tank level percentage
- **Battery Voltage** - Vehicle battery voltage

### Conditions Screen
GPS and tilt sensor data display:
- **GPS Coordinates** - Latitude, longitude, altitude
- **GPS Speed** - Speed from GPS sensor with radial gauge
- **Heading/Course** - Direction of travel with compass gauge
- **Pitch** - Device pitch angle (forward/backward tilt)
- **Roll** - Device roll angle (side-to-side tilt)
- **Yaw** - Device yaw angle (rotation)

## Hardware Requirements

### OBD-II Bluetooth Adapter
- Recommended: Veepeak OBDCheck BLE+ (https://www.amazon.com/dp/B08NFLL3NT)
- Any ELM327-compatible Bluetooth Low Energy OBD-II adapter should work
- Must support standard OBD-II PIDs (Mode 01)

### Device Requirements
- iPad (or iPhone) running iOS 15.0 or later
- Bluetooth Low Energy support
- GPS/Location services
- Accelerometer and Gyroscope sensors

## Setup

### 1. Install Dependencies
The project uses:
- **Plugin.BLE** (v3.1.0) - Bluetooth Low Energy communication
- **Syncfusion.Maui.Gauges** (v28.1.36) - Gauge visualizations

### 2. Configure Syncfusion License
Syncfusion requires a license key for production use. You can get a free community license at:
https://www.syncfusion.com/sales/communitylicense

Add your license key to `MauiProgram.cs` or `App.xaml.cs`:
```csharp
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");
```

### 3. Permissions
The app requires the following permissions (already configured):

**iOS (Info.plist):**
- `NSLocationWhenInUseUsageDescription` - GPS access
- `NSBluetoothAlwaysUsageDescription` - Bluetooth access

**Android (AndroidManifest.xml):**
- `ACCESS_FINE_LOCATION` / `ACCESS_COARSE_LOCATION` - GPS access
- `BLUETOOTH` / `BLUETOOTH_ADMIN` / `BLUETOOTH_SCAN` / `BLUETOOTH_CONNECT` - Bluetooth access

### 4. Deploy to iPad
1. Open `mzWheeler.sln` in Visual Studio 2022/2026
2. Select iOS target
3. Connect your iPad via USB or configure wireless deployment
4. Build and deploy

## Usage

### Connecting to OBD-II Device

1. Plug the OBD-II adapter into your vehicle's OBD port (usually under the dashboard)
2. Turn on vehicle ignition (engine can be off or running)
3. Open mzWheeler app on iPad
4. Go to **Dashboard** tab
5. Tap **Connect** button
6. The app will scan for nearby Bluetooth devices and automatically connect to the OBD adapter
7. Once connected, vehicle data will start updating in real-time

### Monitoring GPS and Tilt

1. Go to **Conditions** tab
2. Tap **Start** button to begin monitoring
3. The app will request location permissions (grant when prompted)
4. GPS coordinates, speed, and heading will display
5. Device tilt (pitch, roll, yaw) will update as you move the iPad

## Architecture

### Services
- **ObdBluetoothService** - Handles BLE communication with OBD-II adapter
- **LocationService** - Provides GPS/location data
- **TiltService** - Accesses accelerometer and gyroscope sensors

### ViewModels (MVVM Pattern)
- **DashboardViewModel** - Manages vehicle telemetry data binding
- **ConditionsViewModel** - Manages GPS and tilt data binding

### Models
- **VehicleData** - Vehicle telemetry properties
- **LocationData** - GPS location properties
- **TiltData** - Device orientation properties

## OBD-II PIDs Supported

The app currently queries these standard OBD-II PIDs:
- `010C` - Engine RPM
- `010D` - Vehicle Speed
- `0104` - Engine Load
- `0105` - Coolant Temperature
- `0111` - Throttle Position
- `012F` - Fuel Tank Level
- `0142` - Control Module Voltage (Battery)

## Troubleshooting

### OBD Connection Issues
- Ensure vehicle ignition is ON
- Check that OBD adapter is properly seated in the port
- Verify OBD adapter is powered (usually has a LED indicator)
- Some vehicles may require the engine to be running
- Try power-cycling the OBD adapter (unplug and replug)

### GPS Not Working
- Ensure location permissions are granted
- Check that Location Services are enabled in iPad Settings
- GPS may take a few moments to acquire initial lock
- Try going outside or near a window for better satellite visibility

### Bluetooth Pairing
- The app uses BLE and does not require manual Bluetooth pairing
- If connection fails, check Bluetooth is enabled in iPad Settings
- Restart Bluetooth if needed
- Some adapters may already be paired to another device

## Future Enhancements

- [ ] Support for more OBD-II PIDs (transmission temp, oil pressure, etc.)
- [ ] Data logging and trip history
- [ ] Configurable gauge ranges and units (imperial/metric)
- [ ] Fault code reading (DTC)
- [ ] Custom gauge layouts
- [ ] Integration with vehicle-specific PIDs

## License

This project is part of the MarketZone solution.

## Credits

- Built with .NET MAUI for cross-platform mobile development
- Gauges powered by Syncfusion.Maui.Gauges
- Bluetooth communication via Plugin.BLE
