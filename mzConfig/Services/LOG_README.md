# Log Service

A lightweight, cross-platform logging service for .NET MAUI applications that wraps `Debug.WriteLine` with structured log levels and automatic caller information.

## Features

- **Four log levels**: Debug, Info, Warning, Error
- **Automatic caller information**: Method name, file name, and line number are captured automatically
- **Exception logging**: Dedicated method with inner exception and stack trace support
- **Visual separators**: Create dividing lines in logs for better readability
- **Configurable**: Enable/disable logging and set minimum log level
- **Cross-platform**: Works on all .NET MAUI platforms (Windows, Android, iOS, macOS)

## Usage

### Basic Logging

```csharp
using mzConfigure.Services;

// Debug - Detailed diagnostic information
Log.Debug("Loading user preferences");

// Info - General operational messages
Log.Info($"Connected to server at {url}");

// Warning - Recoverable issues or unexpected conditions
Log.Warning("API returned empty response, using cached data");

// Error - Failures and critical issues
Log.Error("Failed to save file to disk");
```

### Exception Logging

```csharp
try
{
	await ConnectToServer();
}
catch (Exception ex)
{
	// Logs exception type, message, inner exception, and stack trace
	Log.Exception(ex, "Connection failed");
}
```

### Visual Separators

```csharp
// Creates a separator line
Log.Separator("StartApplication");

// Output in log:
// === StartApplication ========================================================
```

### Configuration

```csharp
// Disable all logging
Log.IsEnabled = false;

// Set minimum log level (only Warning and Error will be logged)
Log.MinimumLevel = Log.LogLevel.Warning;
```

## Log Output Format

Each log entry includes:
- **Timestamp**: HH:mm:ss.fff format
- **Level**: DEBUG, INFO, WARNING, ERROR
- **Source**: [FileName.MethodName]
- **Message**: Your log message

Example output:
```
[14:23:45.123] INFO    [MainViewModel.ShowConnectDialog] User entered URL: http://10.42.0.1:8765
[14:23:45.456] INFO    [MainViewModel.ShowConnectDialog] Testing connection to http://10.42.0.1:8765...
[14:23:46.789] INFO    [MainViewModel.LoadSpecials] Received 5 specials from API
```

## Viewing Logs

### Visual Studio (Windows)
1. Open **View > Output**
2. Select **Debug** from the dropdown
3. All logs will appear in real-time

### Visual Studio for Mac
1. Open **View > Pads > Application Output**
2. Logs appear in the Debug output panel

### Android
- Logs appear in the Android Device Log (Logcat)
- Filter by your app package name

### iOS
- Logs appear in the Device Log window
- Or use Xcode's Console app

## Best Practices

1. **Use appropriate levels**:
   - `Debug`: Verbose diagnostic data
   - `Info`: Normal application flow
   - `Warning`: Recoverable issues
   - `Error`: Failures requiring attention

2. **Use separators for major operations**:
   ```csharp
   Log.Separator("ProcessPayment: Starting");
   // ... operation code ...
   Log.Separator("ProcessPayment: Completed");
   ```

3. **Include context in messages**:
   ```csharp
   // Good
   Log.Info($"Loading {count} items from cache");

   // Less useful
   Log.Info("Loading items");
   ```

4. **Use Log.Exception for all exceptions**:
   ```csharp
   catch (Exception ex)
   {
	   Log.Exception(ex, "Operation context");
   }
   ```

## Performance

- Minimal overhead when logging is enabled
- Zero overhead when `Log.IsEnabled = false`
- Caller information is captured via compiler attributes (no reflection)

## Platform Support

Works identically on all .NET MAUI platforms:
- ✅ Windows
- ✅ Android
- ✅ iOS
- ✅ macOS
- ✅ Tizen

## Implementation Details

The Log service uses:
- `System.Diagnostics.Debug.WriteLine` for output
- `[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]` attributes for automatic caller info
- Static methods for zero-allocation, simple usage
