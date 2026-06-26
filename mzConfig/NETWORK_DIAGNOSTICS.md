# Network Diagnostics Feature

## Overview
Added comprehensive network diagnostics to help troubleshoot connectivity issues between the Android device and the Raspberry Pi.

## What's New

### 1. **Network Diagnostics Service**
A new service that provides:
- **Ping functionality** - Tests basic ICMP connectivity to the Pi
- **TCP fallback** - If ping fails (Android restrictions), tries TCP connection on port 8765
- **Local IP detection** - Shows your device's current IP address
- **Network type detection** - Shows if you're on WiFi, Cellular, etc.
- **Internet availability check** - Confirms if the device has internet access

### 2. **Diagnostics Button**
A new "Diagnostics" button has been added to the main page toolbar (cyan/teal color) that runs a comprehensive network diagnostic report.

## How to Use

### Running Network Diagnostics
1. Click the **"Diagnostics"** button (cyan colored, between WiFi and orientation switch)
2. Wait for the diagnostics to complete (a few seconds)
3. A dialog will show you:
   - Your device's local IP address
   - Network type (WiFi, Cellular, etc.)
   - Ping results to the Pi
   - Any connectivity issues detected

### Reading the Report

#### ✅ Successful Connection
```
=== Network Diagnostics ===

Target: 10.42.0.1

✓ Connected to WiFi
✓ Local IP: 192.168.1.100
✓ Can reach 10.42.0.1
✓ Ping time: 25ms
```

#### ❌ Connection Issues
```
=== Network Diagnostics ===

Target: 10.42.0.1

Network Issues Detected:
Local IP: 192.168.1.100
Network: WiFi
✗ Cannot reach 10.42.0.1: Connection timeout

--- Technical Details ---
Network error connecting to http://10.42.0.1:8765: Connection refused. 
Check if the Pi is powered on and you're on the correct network.
```

## Common Issues & Solutions

### Issue: "Ping not available, trying TCP connection test..."
**Cause:** Android 9+ restricts ICMP ping for security  
**Solution:** The app automatically falls back to TCP connection test. This is normal and works fine.

### Issue: "Cannot reach 10.42.0.1: Connection timeout"
**Check:**
- Is the Pi powered on?
- Is the Pi's web server running? (port 8765)
- Are you on the same network as the Pi?
- Is the IP address correct in your settings?

### Issue: "Device does not have a local IP address"
**Check:**
- Is WiFi enabled on your device?
- Are you connected to a WiFi network?
- Try toggling WiFi off and back on

### Issue: "No internet connectivity detected"
**Note:** You don't need internet to connect to the Pi via local network  
**Action:** This is informational only. If your Pi is on the local network, you can still connect.

## Technical Details

### Android Permissions
The following permissions are used (already configured):
- `ACCESS_NETWORK_STATE` - Check network connectivity
- `INTERNET` - Make network requests
- `ACCESS_WIFI_STATE` - Check WiFi status

### Network Types Detected
- **WiFi** - Connected via WiFi
- **Cellular** - Connected via mobile data
- **Ethernet** - Connected via USB/Ethernet adapter
- **Bluetooth** - Connected via Bluetooth tethering
- **Other** - Other network types
- **Not Connected** - No network connection

### Ping vs TCP Connection
- **ICMP Ping**: Fast, accurate latency measurement (may be restricted on Android 9+)
- **TCP Connection**: Fallback method, connects to port 8765, slightly slower but works everywhere

## Integration with Existing Features

The network diagnostics complements your existing troubleshooting tools:
1. **Connect Button** - Basic connection test (no diagnostics)
2. **Diagnostics Button** - Comprehensive network analysis
3. **Enhanced Error Messages** - All API calls now show detailed error messages

## When to Use

- Before connecting to a new Pi
- When connection fails repeatedly
- When you're unsure if you're on the same network
- When the Pi seems unreachable
- To verify your local network setup

## Example Workflow

1. Connect to the "MarketZone" WiFi hotspot
2. Click **Diagnostics** to verify connectivity
3. If successful, click **Connect** to configure the URL
4. Start managing your specials!

---

**Note:** If your app is currently running, you may need to **hot reload** or **restart** to see the new Diagnostics button.
