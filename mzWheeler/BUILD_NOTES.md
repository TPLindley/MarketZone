# mzWheeler Build Notes

## Project Status: ✅ Build Successful

The mzWheeler MAUI application has been successfully created and builds without errors.

## What Was Built

### Application Structure
- **Project Type**: .NET 10 MAUI Multi-platform App
- **Target Platforms**: Android, iOS/iPadOS, Windows, MacCatalyst
- **Architecture**: MVVM pattern with service-based sensor/API layers

### Core Features Implemented

#### 1. Dashboard Screen (`DashboardPageSimple.xaml`)
Displays real-time vehicle telemetry from OBD-II Bluetooth adapter:
- **Speed** - Large gauge with progress bar (km/h, 0-200 range)
- **RPM** - Engine RPM with progress bar (0-8000 range)
- **Engine Load** - Percentage display with progress bar
- **Throttle Position** - Percentage display with progress bar
- **Coolant Temperature** - Temperature in °C
- **Fuel Level** - Percentage remaining
- **Battery Voltage** - Vehicle battery voltage

**UI Style**: Dark theme (#1a1a1a background) with color-coded metrics and modern card layout using `Border` controls

#### 2. Conditions Screen (`ConditionsPageSimple.xaml`)
Displays GPS location and device tilt/orientation data:
- **GPS Coordinates** - Latitude, Longitude, Altitude, Accuracy
- **GPS Speed** - Speed from GPS sensor with progress bar
- **Heading/Course** - Direction of travel (degrees + compass direction: N, NE, E, etc.)
- **Pitch** - Forward/backward tilt angle
- **Roll** - Side-to-side tilt angle  
- **Yaw** - Rotation angle

**UI Style**: Matching dark theme with color-coded sensor cards

### Services Layer

#### ObdBluetoothService.cs
- **Plugin.BLE** integration for Bluetooth Low Energy
- Device scanning and automatic connection
- OBD-II command transmission over BLE
- Response parsing for standard PIDs:
  - `010C` - Engine RPM
  - `010D` - Vehicle Speed
  - `0104` - Engine Load
  - `0105` - Coolant Temperature
  - `0111` - Throttle Position
  - `012F` - Fuel Tank Level
  - `0142` - Control Module Voltage
- Real-time data polling with configurable interval (500ms default)
- Connection status events

#### LocationService.cs
- MAUI `Geolocation` API wrapper
- Permission checking and request
- One-time location queries
- Continuous monitoring mode
- Location accuracy settings
- Status events for UI feedback

#### TiltService.cs
- MAUI `Accelerometer` and `Gyroscope` sensor access
- Continuous monitoring of device orientation
- Calculates pitch, roll, and yaw angles
- Real-time sensor data streaming
- Status events

### ViewModels (MVVM)

#### DashboardViewModel.cs
- Binds vehicle telemetry data to dashboard UI
- Connect/Disconnect commands for OBD adapter
- Connection status tracking
- Real-time property updates via INotifyPropertyChanged

#### ConditionsViewModel.cs
- Binds GPS and tilt data to conditions UI
- Start/Stop monitoring commands
- Compass direction calculation (N, NE, E, SE, S, SW, W, NW)
- Unit conversions (m/s to km/h for speed)
- Combined monitoring status display

### Data Models

#### VehicleData.cs
Properties: Speed, Rpm, EngineLoad, Throttle, CoolantTemp, FuelLevel, BatteryVoltage, FuelConsumption, Timestamp

#### LocationData.cs
Properties: Latitude, Longitude, Altitude, Speed, Course, Accuracy, Timestamp

#### TiltData.cs
Properties: AccelerometerX/Y/Z, GyroscopeX/Y/Z, Pitch, Roll, Yaw, Timestamp

### Value Converters

#### InvertedBoolConverter.cs
Inverts boolean values for button enable/disable states

#### ProgressConverter.cs
Converts numeric values to 0.0-1.0 range for `ProgressBar` controls
- Takes max value as converter parameter
- Used for gauges: Speed/200, RPM/8000, Load/100, etc.

## Platform Permissions Configured

### Android (`AndroidManifest.xml`)
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
```

### iOS/iPadOS (`Info.plist`)
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs access to your location to display GPS data and track your vehicle position.</string>

<key>NSBluetoothAlwaysUsageDescription</key>
<string>This app needs Bluetooth access to connect to your OBD-II vehicle diagnostic device.</string>
```

## Dependencies

### NuGet Packages
- **Plugin.BLE** v3.1.0 - Bluetooth Low Energy for OBD-II communication
- **Syncfusion.Maui.Gauges** v28.1.36 - Installed but not currently used (simplified UI uses native MAUI controls instead)

### Syncfusion Note
The Syncfusion gauges package is included but the current implementation uses standard MAUI controls (`ProgressBar`, `Border`, `Label`) for compatibility. The complex Syncfusion gauge XAML encountered parsing issues with .NET 10 MAUI source generation. The original Syncfusion-based pages are preserved as:
- `Views/DashboardPage.xaml` (removed - had XAML issues)
- `Views/ConditionsPage.xaml` (removed - had XAML issues)

Current simplified pages:
- `Views/DashboardPageSimple.xaml` ✅
- `Views/ConditionsPageSimple.xaml` ✅

## Navigation

### AppShell.xaml
Tab-based navigation with two tabs:
1. **Dashboard** - Vehicle telemetry
2. **Conditions** - GPS and tilt

## Build Configuration

### Successful Build
```
Platform: net10.0-android
Configuration: Debug
Result: ✅ Build succeeded in 73.9s
Output: bin\Debug\net10.0-android\mzWheeler.dll
```

### Multi-targeting
The project targets:
- `net10.0-android` (Android 21+)
- `net10.0-ios` (iOS 15.0+)
- `net10.0-maccatalyst` (MacCatalyst 15.0+)
- `net10.0-windows10.0.19041.0` (Windows 10, version 2004+)

## Next Steps for Development

### 1. Hardware Testing
- Test with actual OBD-II BLE adapter (Veepeak OBDCheck BLE+)
- Verify OBD command/response format matches real device
- Test GPS accuracy on physical iPad
- Test accelerometer/gyroscope orientation calculations

### 2. Syncfusion Licensing (if needed)
If you decide to use the Syncfusion gauges later, register license key:
```csharp
// In MauiProgram.cs or App.xaml.cs
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");
```
Get free community license at: https://www.syncfusion.com/sales/communitylicense

### 3. OBD Response Parsing Refinement
The current `ObdBluetoothService.ParseObdResponse` method is a basic implementation. Real OBD adapters may:
- Return different response formats
- Include extra whitespace or headers
- Use different line endings
- Require initialization commands (ATZ, ATE0, etc.)

### 4. Additional Features
- **Data Logging** - Save trip data to SQLite
- **Fault Codes** - Read/clear DTCs (Mode 03/04)
- **Freeze Frame Data** - Capture sensor snapshot when fault occurs
- **Unit Preferences** - Toggle km/h vs mph, °C vs °F
- **Gauge Customization** - Allow user to rearrange dashboard
- **Dark/Light Theme** - Theme switching
- **Vehicle Profiles** - Support multiple vehicles with different OBD capabilities

### 5. iOS Deployment
To deploy to iPad:
1. Configure iOS provisioning profile in Visual Studio
2. Connect iPad via USB or set up wireless deployment
3. Build and deploy `net10.0-ios` target
4. Grant location and Bluetooth permissions when prompted

### 6. OBD Adapter Setup
When using the Veepeak adapter:
1. Plug into vehicle's OBD-II port (usually under dashboard near steering column)
2. Turn ignition to ON (engine can be off for testing, running for real data)
3. Adapter LED should illuminate (indicates power)
4. Open mzWheeler app
5. Tap "Connect" - app will scan and connect automatically
6. Data should start streaming

## Known Limitations

1. **Yaw Calculation**: The yaw angle in `TiltService` is approximate (gyroscope integration). For accurate heading, use GPS course instead.

2. **OBD Compatibility**: Not all vehicles support all PIDs. Older vehicles may not support fuel level or battery voltage PIDs.

3. **BLE Range**: Bluetooth range is limited (~10 meters). OBD adapter must stay within range.

4. **GPS Accuracy**: Indoor GPS may be inaccurate. Best used outdoors with clear sky view.

5. **Sensor Availability**: Some devices may not have gyroscope. Handle `FeatureNotSupportedException` gracefully.

## Files Created

### Core Files
- `mzWheeler/mzWheeler.csproj` - Project definition with multi-targeting
- `mzWheeler/MauiProgram.cs` - App initialization with Syncfusion setup
- `mzWheeler/App.xaml` - Resource dictionary with converters
- `mzWheeler/AppShell.xaml` - Tab navigation structure

### Models
- `Models/VehicleData.cs`
- `Models/LocationData.cs`
- `Models/TiltData.cs`

### Services
- `Services/ObdBluetoothService.cs`
- `Services/LocationService.cs`
- `Services/TiltService.cs`

### ViewModels
- `ViewModels/DashboardViewModel.cs`
- `ViewModels/ConditionsViewModel.cs`

### Views
- `Views/DashboardPageSimple.xaml` + `.cs`
- `Views/ConditionsPageSimple.xaml` + `.cs`

### Converters
- `Converters/InvertedBoolConverter.cs`
- `Converters/ProgressConverter.cs`

### Platform Configuration
- `Platforms/Android/AndroidManifest.xml` (permissions added)
- `Platforms/iOS/Info.plist` (permissions added)

### Documentation
- `README.md` - User guide and setup instructions
- `BUILD_NOTES.md` (this file) - Technical implementation details

## Testing Checklist

- [ ] Build Android APK
- [ ] Build iOS IPA
- [ ] Deploy to iPad
- [ ] Test Bluetooth permission request
- [ ] Test Location permission request
- [ ] Scan for OBD adapter
- [ ] Connect to OBD adapter
- [ ] Verify speed updates
- [ ] Verify RPM updates
- [ ] Verify engine load updates
- [ ] Test GPS location acquisition
- [ ] Test GPS speed accuracy
- [ ] Test heading/compass direction
- [ ] Test pitch/roll/yaw sensors
- [ ] Test disconnect/reconnect flow
- [ ] Test start/stop monitoring
- [ ] Test app backgrounding behavior
- [ ] Verify no memory leaks during long sessions

## Color Scheme

Dashboard metrics use distinct colors for quick identification:
- **Speed**: `#4CAF50` (Green)
- **RPM**: `#2196F3` (Blue)
- **Engine Load**: `#9C27B0` (Purple)
- **Throttle**: `#FF9800` (Orange)
- **Coolant**: `#00BCD4` (Cyan)
- **Fuel**: `#FFC107` (Amber)
- **Battery**: `#8BC34A` (Light Green)
- **Pitch**: `#9C27B0` (Purple)
- **Roll**: `#FF9800` (Orange)
- **Yaw**: `#00BCD4` (Cyan)

Background: `#1a1a1a` (Dark)  
Cards: `#2a2a2a` (Slightly lighter dark)  
Text: `White` for primary, `#888` for secondary

---

**Build Date**: January 2025  
**Framework**: .NET 10 MAUI  
**Status**: ✅ Ready for hardware testing
