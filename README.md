# MarketZone - Rolling Pin Bakery Display System

A complete digital signage solution for displaying daily specials at Rolling Pin Bakery, consisting of a Raspberry Pi display application and a mobile configuration app.

## System Components

### 1. mzSpecials (Raspberry Pi Display App)
- **Language**: C++ with GTK+
- **Platform**: Raspberry Pi (Linux)
- **Function**: Displays scrolling specials on a connected screen
- **Features**:
  - Full-screen display with custom branding
  - Smooth scrolling animation
  - Custom colors for each special
  - REST API for remote configuration
  - Persistent storage of specials

### 2. mzConfig (Mobile Configuration App)
- **Framework**: .NET 10 MAUI
- **Platforms**: Android, iOS, Windows, macOS
- **Function**: Remote configuration interface for the display
- **Features**:
  - Add, edit, and remove specials
  - Custom text and color for each item
  - Load current display state
  - Clear entire display
  - Update display remotely
  - Connection testing
  - Persistent URL configuration

## Architecture

```
┌─────────────────────┐
│   Mobile Device     │
│   (mzConfig App)    │
│                     │
│  - Edit Specials    │
│  - Choose Colors    │
│  - Update Display   │
└──────────┬──────────┘
           │
           │ HTTP REST API
           │ (Port 8765)
           │
┌──────────▼──────────┐
│   Raspberry Pi      │
│   (mzSpecials)      │
│                     │
│  - Display Manager  │
│  - API Server       │
│  - Data Storage     │
└──────────┬──────────┘
           │
           │ HDMI
           │
┌──────────▼──────────┐
│   Display Screen    │
│                     │
│  Rolling Pin Bakery │
│  ═══════════════    │
│  • Special 1        │
│  • Special 2        │
│  • Special 3        │
│  ...                │
└─────────────────────┘
```

## API Specification

All endpoints are hosted on the Raspberry Pi at `http://<raspberry-pi-ip>:8765`

### Endpoints

#### GET /specials
Retrieve the current list of specials displayed on the screen.

**Response**:
```json
[
  {
    "text": "Fresh Croissants - $3.99",
    "color": "#FFFFFF"
  },
  {
    "text": "Daily Sourdough Bread",
    "color": "#FF1595"
  }
]
```

#### DELETE /specials
Clear all specials from the display.

**Response**:
```json
{
  "status": "cleared"
}
```

#### POST /specials
Update the display with a new list of specials.

**Request Body**:
```json
[
  {
    "text": "Fresh Croissants - $3.99",
    "color": "#FFFFFF"
  },
  {
    "text": "Daily Sourdough Bread",
    "color": "#FF1595"
  }
]
```

**Response**:
```json
{
  "status": "ok",
  "count": 2
}
```

## Setup Instructions

### Raspberry Pi Setup (mzSpecials)

1. **Install Dependencies**:
```bash
sudo apt-get update
sudo apt-get install build-essential cmake
sudo apt-get install libgtkmm-3.0-dev
```

2. **Build the Application**:
```bash
cd mzSpecials
mkdir build
cd build
cmake ..
make
```

3. **Run the Application**:
```bash
./mzSpecials
```

4. **Auto-start on Boot** (Optional):
```bash
# Create a systemd service
sudo nano /etc/systemd/system/mzspecials.service

# Add the following content:
[Unit]
Description=MarketZone Specials Display
After=graphical.target

[Service]
Type=simple
User=pi
Environment=DISPLAY=:0
ExecStart=/home/pi/mzSpecials/build/mzSpecials
Restart=on-failure

[Install]
WantedBy=graphical.target

# Enable and start the service
sudo systemctl enable mzspecials.service
sudo systemctl start mzspecials.service
```

### Mobile App Setup (mzConfig)

1. **Prerequisites**:
   - Visual Studio 2026 or later
   - .NET 10 SDK
   - MAUI workload

2. **Build**:
```bash
cd mzConfig
dotnet build
```

3. **Deploy**:
   - **Android**: Connect device via USB and deploy from Visual Studio
   - **iOS**: Connect device and deploy from Visual Studio (requires Mac)
   - **Windows**: Run directly from Visual Studio
   - **macOS**: Run directly from Visual Studio

## Network Configuration

### Finding Your Raspberry Pi

The mobile app needs to know the Raspberry Pi's network address:

1. **By Hostname** (if mDNS is working):
   - URL: `http://raspberrypi.local:8765`

2. **By IP Address**:
   - Find the IP: `hostname -I` on the Raspberry Pi
   - URL: `http://192.168.1.XXX:8765`

### Firewall Configuration

Ensure the API port is accessible:
```bash
sudo ufw allow 8765/tcp
```

## Color Codes

The system uses hex color codes for custom text colors:

| Color   | Hex Code  |
|---------|-----------|
| White   | `#FFFFFF` |
| Pink    | `#FF1595` |
| Red     | `#FF0000` |
| Orange  | `#FF8C00` |
| Yellow  | `#FFFF00` |
| Lime    | `#00FF00` |
| Cyan    | `#00FFFF` |
| Blue    | `#0000FF` |
| Purple  | `#800080` |
| Magenta | `#FF00FF` |

## Typical Workflow

1. **Initial Setup**:
   - Start mzSpecials on Raspberry Pi
   - Install mzConfig on mobile device
   - Configure Raspberry Pi URL in the mobile app
   - Test connection

2. **Daily Operations**:
   - Open mzConfig on mobile device
   - Load current specials (or start fresh)
   - Add/edit specials for the day
   - Choose colors for emphasis
   - Update display
   - Verify on the physical screen

3. **Clearing Display**:
   - Open mzConfig
   - Tap "Clear Display"
   - Confirm action

## Troubleshooting

### Display App Not Starting
- Check GTK dependencies are installed
- Verify display is connected and configured
- Check system logs: `journalctl -u mzspecials.service`

### Cannot Connect from Mobile
- Verify Raspberry Pi and mobile device are on same network
- Check firewall settings on Raspberry Pi
- Try using IP address instead of hostname
- Ensure mzSpecials is running and API server is active

### Colors Not Showing
- Verify hex color format: `#RRGGBB`
- Check display supports the color
- Try common colors (white, red, etc.) first

### Specials Not Updating
- Check network connectivity
- Verify API is responding (use Test Connection)
- Check Raspberry Pi logs for errors
- Ensure changes are saved before updating

## Development

### Project Structure
```
MarketZone/
├── mzSpecials/              # Raspberry Pi C++ Application
│   ├── src/
│   │   └── main.cpp        # Main application logic
│   ├── include/
│   │   ├── httplib.h       # HTTP server library
│   │   └── json.hpp        # JSON parsing library
│   └── CMakeLists.txt      # Build configuration
│
├── mzConfig/                # .NET MAUI Mobile App
│   ├── Models/
│   │   └── Special.cs      # Data model
│   ├── Services/
│   │   └── SpecialsApiService.cs  # API client
│   ├── ViewModels/
│   │   ├── MainViewModel.cs       # Main logic
│   │   ├── ColorPickerViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── MainPage.xaml       # Main UI
│   ├── MainPage.xaml.cs    # Code-behind
│   └── mzConfig.csproj     # Project file
│
└── .gitignore              # Git ignore rules
```

### Technologies Used

**mzSpecials**:
- C++17
- GTK+ 3.0 / gtkmm
- Pango (text rendering)
- cpp-httplib (HTTP server)
- nlohmann/json (JSON parsing)

**mzConfig**:
- .NET 10
- MAUI (Multi-platform App UI)
- MVVM pattern
- System.Net.Http
- System.Text.Json

## License

Copyright © 2026 Terminal Solutions

## Support

For issues or questions, please contact Terminal Solutions support.
