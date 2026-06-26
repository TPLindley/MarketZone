# MarketZone Configuration App

A .NET MAUI mobile application for configuring the Rolling Pin Bakery specials display on a Raspberry Pi.

## Overview

This mobile app provides a user-friendly interface to manage the specials displayed on a Raspberry Pi-powered digital signage system. The app communicates with the `mzSpecials` application running on the Raspberry Pi via a REST API.

## Features

### Connection Management
- Configure the Raspberry Pi URL and port
- Test connection to ensure the device is reachable
- Default connection: `http://raspberrypi.local:8765`

### Display Operations
- **Load from Display**: Retrieve the current list of specials from the Raspberry Pi
- **Clear Display**: Remove all specials from the display
- **Update Display**: Send the current list of specials to the Raspberry Pi

### Specials Management
- Add new special items with custom text and color
- Edit existing specials (text and color)
- Remove unwanted specials
- Visual color preview for each special
- Color format: Hex color codes (e.g., `#FF1595`, `#FFFFFF`)

## API Endpoints

The app communicates with three REST API endpoints on the Raspberry Pi:

1. **GET /specials** - Retrieve current display content
   - Returns: JSON array of specials with `text` and `color` properties

2. **DELETE /specials** - Clear/reset the display
   - Returns: Status confirmation

3. **POST /specials** - Update the display
   - Body: JSON array of specials `[{"text": "...", "color": "#RRGGBB"}, ...]`
   - Returns: Status and count of updated items

## Project Structure

```
mzConfig/
├── Models/
│   └── Special.cs              # Data model for special items
├── Services/
│   └── SpecialsApiService.cs   # HTTP client for API communication
├── ViewModels/
│   └── MainViewModel.cs        # MVVM ViewModel with business logic
├── MainPage.xaml               # UI layout
└── MainPage.xaml.cs            # Code-behind
```

## Usage

1. **Configure Connection**
   - Enter the Raspberry Pi URL (default: `http://raspberrypi.local:8765`)
   - Tap "Test Connection" to verify connectivity

2. **Load Current Specials**
   - Tap "Load from Display" to fetch the current specials from the Raspberry Pi
   - This will populate the list with existing items

3. **Edit Specials**
   - Tap "+ Add" to create a new special
   - Edit the text and color fields directly in the list
   - Use hex color codes (e.g., `#FF1595` for pink)
   - Tap the ✕ button to remove a special

4. **Update the Display**
   - After making changes, tap "Update Display" to send the new list to the Raspberry Pi
   - The display will update immediately

5. **Clear the Display**
   - Tap "Clear Display" to remove all specials from the Raspberry Pi
   - Requires confirmation

## Technical Details

- **Framework**: .NET 10 MAUI
- **Platforms**: Android, iOS, macOS, Windows
- **Architecture**: MVVM (Model-View-ViewModel)
- **HTTP Client**: System.Net.Http with JSON serialization
- **UI Pattern**: Data binding with ObservableCollection

## Color Format

Colors are specified using hex color codes:
- Format: `#RRGGBB` (e.g., `#FF1595`)
- Common colors:
  - White: `#FFFFFF`
  - Pink: `#FF1595`
  - Red: `#FF0000`
  - Green: `#00FF00`
  - Blue: `#0000FF`
  - Yellow: `#FFFF00`

## Network Configuration

### Finding Your Raspberry Pi
- By hostname: `http://raspberrypi.local:8765`
- By IP address: `http://192.168.1.XXX:8765`

### Firewall Considerations
Ensure port 8765 is accessible on the Raspberry Pi:
```bash
sudo ufw allow 8765/tcp
```

## Troubleshooting

### Cannot Connect to Raspberry Pi
- Verify the Raspberry Pi is on the same network
- Check the IP address or hostname is correct
- Ensure the mzSpecials application is running on the Pi
- Test connectivity using "Test Connection" button
- Try using IP address instead of hostname

### Colors Not Displaying Correctly
- Ensure colors are in hex format: `#RRGGBB`
- Use a color picker tool to get valid hex codes
- Default color is white (`#FFFFFF`)

### Changes Not Appearing on Display
- Make sure to tap "Update Display" after editing
- Check the status message for errors
- Verify network connectivity

## Development

### Prerequisites
- Visual Studio 2026 or later
- .NET 10 SDK
- MAUI workload installed

### Building
```bash
dotnet build mzConfig/mzConfig.csproj
```

### Running
- Android: Deploy to physical device or emulator
- iOS: Deploy to physical device or simulator (requires Mac)
- Windows: Run directly on Windows 10/11
- macOS: Run directly on macOS

## Related Projects

- **mzSpecials**: Raspberry Pi C++ application that displays the specials on a connected screen
