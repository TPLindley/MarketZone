# Portrait/Landscape Orientation Feature

## Overview
The mzConfig app now includes a simple switch control to toggle the Raspberry Pi display between Portrait and Landscape orientations.

## UI Location
The orientation switch is located in the **action button row** at the top of the specials list, between the WiFi button and the Connect button.

## Visual Design
```
┌──────────────────────────────────────────────────────────┐
│  Add  Update  Load  Clear  Library  Header  WiFi         │
│                                                           │
│  [Landscape ⚪]  [Connect]                               │
└──────────────────────────────────────────────────────────┘
```

When toggled:
```
┌──────────────────────────────────────────────────────────┐
│  Add  Update  Load  Clear  Library  Header  WiFi         │
│                                                           │
│  [Portrait 🟣]  [Disconnect]                             │
└──────────────────────────────────────────────────────────┘
```

## Features

### Switch Control
- **Label**: Shows current orientation ("Landscape" or "Portrait")
- **Switch**: Toggle between modes
- **Pink color** (#FF00FF) when toggled on (Portrait)
- **White thumb** for visibility
- **Only enabled when connected** to PI

### Behavior
1. **On Load**: App fetches current orientation from PI
2. **On Toggle**: 
   - Sends POST request to `/orientation` endpoint
   - PI physically rotates display using xrandr
   - Shows status message
3. **On Error**: Reverts switch and shows error dialog

### Smart Loading Prevention
- Uses `_isLoadingOrientation` flag to prevent infinite loops
- Prevents API call when loading initial state
- Only sends API call when user manually toggles

## Technical Implementation

### API Endpoints (Raspberry Pi)
- **GET /orientation** - Returns `{"orientation": "landscape"}` or `{"orientation": "portrait"}`
- **POST /orientation** - Accepts `{"orientation": "landscape"}` or `{"orientation": "portrait"}`

### MAUI Components

**MainPage.xaml** (Lines 96-107)
```xml
<HorizontalStackLayout Spacing="4" VerticalOptions="Center">
	<Label
		Text="{Binding OrientationText}"
		FontSize="12"
		VerticalOptions="Center"/>
	<Switch
		IsToggled="{Binding IsPortrait}"
		IsEnabled="{Binding IsConnected}"
		VerticalOptions="Center"
		OnColor="#FF00FF"
		ThumbColor="White"/>
</HorizontalStackLayout>
```

**MainViewModel.cs**
- `IsPortrait` property (lines 168-183)
- `OrientationText` property (line 185)
- `UpdateOrientation()` method (lines 667-692)
- `LoadSpecials()` loads orientation (lines 253-266)

**SpecialsApiService.cs**
- `GetOrientationAsync()` - GET endpoint
- `SetOrientationAsync(string)` - POST endpoint
- `OrientationInfo` class with JSON serialization

## User Flow

### First Connection
1. App connects to PI
2. Fetches current orientation
3. Switch reflects PI's current state

### Changing Orientation
1. User toggles switch
2. Status shows "Setting orientation to [Portrait/Landscape]..."
3. API call sent to PI
4. PI rotates display using xrandr command
5. Status shows "Orientation set to [Portrait/Landscape]"
6. Switch updates to reflect new state

### Error Handling
- If API call fails, switch reverts to previous position
- Error dialog shows user-friendly message
- Status bar shows error details

## Physical Display Rotation

The Raspberry Pi uses **xrandr** to physically rotate the display:
- **Landscape**: `xrandr --output [display] --rotate normal`
- **Portrait**: `xrandr --output [display] --rotate right`

The rotation is immediate and affects:
- The GTK window layout
- All displayed content (header, specials, scrolling)
- Physical screen orientation

## Persistence

The orientation preference is saved on the Raspberry Pi:
- File: `~/.local/share/mzSpecials/orientation.txt`
- Content: `landscape` or `portrait`
- Loaded on app startup
- Restored after reboot

## Status Messages

### Success
- "Setting orientation to Landscape..."
- "Orientation set to Landscape"
- "Setting orientation to Portrait..."
- "Orientation set to Portrait"

### Errors
- "Error: no connected xrandr output found"
- "Error: Failed to set orientation: [details]"
- "Error: [network/connection errors]"

## Testing Checklist

- [ ] Switch disabled when not connected
- [ ] Switch enabled after successful connection
- [ ] Switch reflects PI's current orientation on load
- [ ] Toggle to Portrait rotates display right
- [ ] Toggle to Landscape rotates display normal
- [ ] Status messages appear during operation
- [ ] Error dialog appears on failure
- [ ] Switch reverts on error
- [ ] Orientation persists after PI reboot
- [ ] Works when connected via WiFi hotspot
- [ ] Works when connected via local network

## Platform Compatibility

### Raspberry Pi
- ✅ Full support with xrandr
- ✅ Physical display rotation
- ✅ Persistence

### Desktop Testing (No PI)
- ⚠️ API calls will fail gracefully
- ⚠️ Error message shown
- ⚠️ Switch remains functional in UI

## Future Enhancements (Ideas)

- Add rotation animation preview
- Display current resolution in each mode
- Auto-detect optimal orientation
- Quick-rotate button (90° increments)
- Rotation lock toggle
- Notification when rotation complete
- Preview mode before committing

## Known Limitations

1. **xrandr Required**: PI must have xrandr installed and configured
2. **Connected Display**: Only works with physically connected displays
3. **Single Display**: Assumes one display output
4. **No Animation**: Rotation is instant, no transition effect
5. **UI Freeze**: Brief UI freeze during rotation (GTK window redraw)

## Troubleshooting

### Switch Not Appearing
- Rebuild and restart the app
- Verify you're on latest Develop branch
- Check MainPage.xaml lines 96-107

### Switch Disabled
- Ensure you're connected to the PI
- Check Connect button shows "Disconnect" (red)

### Rotation Not Working
- Verify PI has xrandr: `xrandr --version`
- Check PI display connection: `xrandr --query`
- Check PI logs: `journalctl -u mzSpecials`

### Switch Reverts Immediately
- API call failed
- Check network connection
- Verify PI is running mzSpecials app
- Check PI firewall settings

### Wrong Initial State
- PI orientation file may be corrupted
- Delete: `~/.local/share/mzSpecials/orientation.txt`
- Restart mzSpecials app
