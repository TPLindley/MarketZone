# Quick Start Guide - mzConfig Mobile App

## First Time Setup

### 1. Install the App
- **Android**: Install the APK or download from app store
- **iOS**: Install from TestFlight or App Store
- **Windows**: Run the installer or launch from Visual Studio

### 2. Configure Connection
1. Launch the mzConfig app
2. In the "Raspberry Pi Connection" section:
   - Enter your Raspberry Pi URL (e.g., `http://192.168.1.100:8765`)
   - Default: `http://raspberrypi.local:8765`
3. Tap **"Test Connection"** to verify it works
4. If successful, you'll see "Connected successfully!"

### 3. Load Current Specials
1. Tap **"Load from Display"** to fetch any existing specials
2. The list will populate with current items from the display

## Daily Usage

### Adding a New Special
1. Tap the **"+ Add"** button in the Specials List section
2. A new special will be added with default text
3. Edit the text field to enter your special (e.g., "Fresh Croissants - $3.99")
4. Edit the color field to change the color (e.g., `#FF1595` for pink)
5. Preview the color using the color box displayed next to the color field

### Editing an Existing Special
1. Tap on the text field of any special to edit it
2. Tap on the color field to change the color
3. Use hex color codes (see color guide below)

### Removing a Special
1. Tap the red **✕** button next to the special you want to remove
2. The special will be removed from the list (but not yet from the display)

### Updating the Display
1. After making all your changes, tap **"Update Display"**
2. The app will send your changes to the Raspberry Pi
3. A confirmation message will show the number of specials updated
4. The display will update immediately

### Clearing the Display
1. Tap **"Clear Display"**
2. Confirm you want to clear all specials
3. All specials will be removed from both the app and the display

## Color Guide

### Common Colors

| Color Name | Hex Code  | Use Case                    |
|------------|-----------|----------------------------|
| White      | `#FFFFFF` | Standard text              |
| Pink       | `#FF1595` | Brand color, highlights    |
| Red        | `#FF0000` | Sale items, urgent         |
| Orange     | `#FF8C00` | Seasonal items             |
| Yellow     | `#FFFF00` | New items                  |
| Lime       | `#00FF00` | Healthy options            |
| Cyan       | `#00FFFF` | Special promotions         |
| Blue       | `#0000FF` | Daily specials             |
| Purple     | `#800080` | Premium items              |
| Magenta    | `#FF00FF` | Limited time offers        |

### How to Use Colors
1. Colors are specified using hex codes: `#RRGGBB`
2. Type the hex code directly in the color field
3. The color preview box shows the actual color
4. If invalid, it defaults to white

### Finding More Colors
Use an online color picker:
- [Google Color Picker](https://www.google.com/search?q=color+picker)
- Copy the hex code (e.g., `#FF1595`)
- Paste into the color field

## Tips & Best Practices

### Creating Effective Specials
- Keep text concise and readable
- Use colors sparingly for emphasis
- Most items should be white for readability
- Use brand pink (`#FF1595`) for important items
- Test readability on the actual display

### Typical Daily Workflow
1. Open the app in the morning
2. Load current specials to see yesterday's items
3. Clear display or edit existing items
4. Add new specials for today
5. Choose colors to highlight key items
6. Update display
7. Verify on the physical screen

### Managing Connection
- The app remembers your Raspberry Pi URL
- You only need to set it up once
- Use "Test Connection" if you have issues
- Try IP address if hostname doesn't work

## Troubleshooting

### "Cannot connect to Raspberry Pi"
**Solutions**:
1. Check your phone/device is on the same Wi-Fi network
2. Verify the Raspberry Pi is powered on
3. Try using the IP address instead of hostname
4. Test connection from a web browser: `http://your-pi-ip:8765/specials`

### "No specials. Add some or load from display."
**This is normal!**
- It means the display is empty or you haven't loaded yet
- Tap "+ Add" to create new specials
- Or tap "Load from Display" to fetch existing ones

### Colors Not Showing on Display
**Check**:
1. Use proper hex format: `#RRGGBB`
2. Test with common colors (white, red) first
3. Verify the display screen supports the color

### Changes Not Appearing
**Make sure**:
1. You tapped "Update Display" after editing
2. Check the status message for errors
3. Test connection is working
4. Try restarting the mzSpecials app on the Raspberry Pi

### App Crashes or Freezes
**Try**:
1. Force close and reopen the app
2. Check your internet connection
3. Verify the Raspberry Pi is responding
4. Update the app if available

## Advanced Features

### Network Configuration
- Default URL: `http://raspberrypi.local:8765`
- Change to IP address: `http://192.168.1.XXX:8765`
- Port: Always use `8765` (unless you changed it on the Pi)

### Saving URL Preferences
- The app automatically saves your Raspberry Pi URL
- It persists even after closing the app
- Update it anytime in the Connection section

### Batch Operations
- Load existing specials before editing
- Make all changes at once
- Single "Update Display" applies everything
- More efficient than individual updates

## Getting Help

### Status Messages
- Watch the status text below the connection section
- Green messages = success
- Red/error messages = something went wrong
- "Loading..." = operation in progress

### Error Messages
The app shows detailed error messages if something fails:
- Read the message carefully
- Check network connection first
- Verify Raspberry Pi is running
- Try testing connection

### Support
If you continue having issues:
1. Take a screenshot of any error messages
2. Note what you were trying to do
3. Contact Terminal Solutions support
4. Have your Raspberry Pi IP address ready

## Example: Setting Up Daily Specials

### Morning Routine
```
1. Open mzConfig app
2. Tap "Load from Display" (see yesterday's items)
3. Tap "Clear Display" and confirm
4. Tap "+ Add" for first special
5. Edit text: "Fresh Croissants - $3.99"
6. Edit color: #FFFFFF (white)
7. Tap "+ Add" for second special
8. Edit text: "Today's Special: Blueberry Muffins"
9. Edit color: #FF1595 (pink)
10. Repeat for all items
11. Tap "Update Display"
12. Check the physical screen
```

### Quick Update (Adding One Item)
```
1. Open mzConfig app
2. Tap "Load from Display" (get current list)
3. Tap "+ Add"
4. Edit the new special text and color
5. Tap "Update Display"
6. Done!
```

## Keyboard Shortcuts (Desktop Only)

When running on Windows or macOS:
- **Tab**: Navigate between fields
- **Enter**: Confirm/submit
- **Esc**: Cancel dialogs

## Privacy & Data

### What's Stored Locally
- Raspberry Pi URL preference
- Nothing else (no personal data)

### Network Communication
- App only communicates with your Raspberry Pi
- No internet connection required (just local network)
- No data sent to external servers

### Security
- Communication is over local network
- Consider using HTTPS if needed (requires setup)
- Keep Raspberry Pi on private network

---

**Need more help?** See the full README.md for detailed documentation.
