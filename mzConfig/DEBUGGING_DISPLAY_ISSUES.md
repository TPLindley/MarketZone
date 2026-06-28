# Debugging Display Update Issues

## Problem
The Port/Land button and Test button appear to send requests successfully, but the Raspberry Pi display does not visibly change. The server accepts the HTTP requests and returns success, but the GTK display may not be applying the changes.

## Changes Made

### 1. Connection Health Checks After Operations
Both `ToggleOrientation()` and `TestAnimation()` now:
- Send the command (orientation change or animation trigger)
- Wait 500ms for the display to process
- Verify the connection is still alive with `TestConnectionAsync()`
- If connection is lost, mark as disconnected and show alert
- Dump full server state for diagnostics

### 2. Server State Diagnostics
Added `GetServerStateAsync()` in `SpecialsApiService.cs`:
- Queries `/specials`, `/header`, and `/orientation` endpoints
- Returns current server state as string
- Called after orientation changes and animation tests
- Logged with `[MZLOG]` prefix for easy filtering

### 3. Enhanced Logging
- `TestConnectionAsync()` now logs the URL, status code, and success/failure
- Connection health check results are logged after operations
- Server state is logged to verify the Pi's reported state matches expectations

## What to Look For in Logs

Filter the Output window by `MZLOG` and look for:

### Successful Orientation Change
```
[MZLOG] ToggleOrientation: Starting
[MZLOG] Sending API call to set orientation to portrait
[MZLOG] POST /orientation: Status=OK, Body={"status":"ok","orientation":"portrait"}
[MZLOG] Verifying connection health after orientation change...
[MZLOG] TestConnection: GET http://10.42.0.1:8765/specials
[MZLOG] TestConnection: Status=OK, Success=True
[MZLOG] Server state after orientation change:
SPECIALS [OK]: [{"text":"Item 1", ...}]
HEADER [OK]: {"text":"My Store","color":"#FF0000"}
ORIENTATION [OK]: {"orientation":"portrait"}
[MZLOG] ToggleOrientation: Success - Button now shows 'Land'
```

### Failed Operation (Connection Lost)
```
[MZLOG] ToggleOrientation: Starting
[MZLOG] Sending API call to set orientation to portrait
[MZLOG] POST /orientation: FAILED after 3 attempts - Connection reset
[MZLOG] ToggleOrientation: FAILED - Exception: Connection reset
[MZLOG] Connection lost after orientation failure
```

### GTK Not Applying Changes (Server OK, Display Not Updating)
```
[MZLOG] POST /orientation: Status=OK, Body={"status":"ok","orientation":"portrait"}
[MZLOG] TestConnection: Status=OK, Success=True
[MZLOG] Server state after orientation change:
ORIENTATION [OK]: {"orientation":"portrait"}
```
If you see this pattern but the display doesn't change, the issue is on the Raspberry Pi side:
- The HTTP server is working
- The orientation value is being updated in memory
- But the GTK main loop may not be processing the `Glib::signal_idle()` callbacks

## Raspberry Pi Side Debugging

### Check mzSpecials Logs
On the Raspberry Pi, the mzSpecials app should be logging:
```
[HTTP] POST /orientation from 10.42.0.175 body={"orientation":"portrait"}
[HTTP] POST /orientation -> set to portrait
[INFO] Orientation layout update scheduled
```

If you see "Orientation layout update scheduled" but the display doesn't change, the GTK event loop may be blocked or the window may not be initialized.

### Verify GTK Display
Check if the mzSpecials display window is:
1. **Visible**: Is the GTK window actually shown on the HDMI output?
2. **Responsive**: Does the window respond to mouse/keyboard input?
3. **Initialized**: Are `g_main_box`, `g_header_box`, and `g_content_box` valid pointers?

### Manual Test (on Raspberry Pi)
SSH into the Pi and run:
```bash
curl -X POST http://localhost:8765/orientation -H "Content-Type: application/json" -d '{"orientation":"portrait"}'
curl -X POST http://localhost:8765/blanking/trigger
```

If these work from the Pi but not from the phone, it's a network/firewall issue.
If these also don't work from the Pi, the display GTK loop is not processing events.

## Next Steps

1. **Run the app** and press Port/Land or Test
2. **Check the Output window** (filter by `MZLOG`)
3. **Verify server state** is being logged correctly
4. **Check connection health** - does it stay connected or drop?
5. **SSH to Pi** and check mzSpecials logs for GTK event processing
6. **Restart mzSpecials** on the Pi if GTK is stuck

## Common Causes

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Connection resets immediately | Pi server crashed or restarted | Restart mzSpecials app |
| 404 on `/blanking/trigger` | Server still starting up | Wait 5 seconds and retry |
| Success logged but display unchanged | GTK not updating | Restart mzSpecials app |
| Connection lost after command | GTK crash/segfault | Check Pi logs for crashes |
| Intermittent failures | Network packet loss | Check WiFi signal strength |

## Testing Strategy

1. **Test basic connectivity first**: Press Connect, verify logs show successful GET /specials
2. **Test orientation**: Press Port/Land, check logs for "Server state after orientation change"
3. **Test animation**: Press Test, check logs for "Server state after animation"
4. **Compare server state**: Does the orientation value in the log match what you requested?
5. **Visually verify display**: Does the Pi's HDMI output actually change layout/show animation?

If logs show success but display doesn't update, the problem is GTK event processing on the Pi.
