# Connecting to PI via WiFi Hotspot

## Overview
The mzConfig app can automatically detect and connect to the Raspberry PI when connected to its WiFi hotspot, eliminating the need to manually configure IP addresses.

## How It Works

### 1. **Connect to PI's WiFi Hotspot**
   - Tap the **"WiFi"** button in the app
   - Connect to "MarketZone" network (password: "Sweet$Treats99")
   - The app will store these credentials securely for future use

### 2. **Automatic IP Detection**
   When connected to the "MarketZone" hotspot, the app will automatically:
   - Try common hotspot gateway IPs in order:
	 - `192.168.4.1` (most common for Raspberry Pi/Linux hotspots)
	 - `192.168.43.1` (Android hotspot default)
	 - `10.0.0.1` (some routers/hotspots)
	 - `192.168.1.1` (generic router default)
   - Test each IP with a quick connection test (2 second timeout)
   - Automatically switch to the first working IP
   - Save this IP for the session

### 3. **Connect to PI Services**
   - After WiFi connection succeeds, tap the **"Connect"** button
   - The app will use the auto-detected IP
   - If auto-detection didn't find the PI, you can manually enter the IP

## Raspberry Pi Hotspot Configuration

To set up your Raspberry Pi as a WiFi hotspot (if not already configured):

### Option 1: Using NetworkManager (Raspberry Pi OS Bookworm+)
```bash
# Create hotspot
nmcli device wifi hotspot ssid "MarketZone" password "Sweet$Treats99"

# Make it persistent
nmcli connection modify Hotspot connection.autoconnect yes
```

### Option 2: Using hostapd
```bash
# Install required packages
sudo apt-get install hostapd dnsmasq

# Configure hostapd (/etc/hostapd/hostapd.conf)
interface=wlan0
driver=nl80211
ssid=MarketZone
hw_mode=g
channel=7
wmm_enabled=0
macaddr_acl=0
auth_algs=1
ignore_broadcast_ssid=0
wpa=2
wpa_passphrase=Sweet$Treats99
wpa_key_mgmt=WPA-PSK
wpa_pairwise=TKIP
rsn_pairwise=CCMP

# Configure static IP (typically 192.168.4.1)
sudo nano /etc/dhcpcd.conf
# Add:
interface wlan0
	static ip_address=192.168.4.1/24
	nohook wpa_supplicant

# Enable and start services
sudo systemctl unmask hostapd
sudo systemctl enable hostapd
sudo systemctl start hostapd
sudo systemctl enable dnsmasq
sudo systemctl start dnsmasq
```

### Option 3: Using RaspAP (easiest)
```bash
curl -sL https://install.raspap.com | bash
# Then configure via web interface at http://10.3.141.1
```

## Default Gateway IPs by Platform

| Platform | Default Gateway IP | Detection Priority |
|----------|-------------------|-------------------|
| Raspberry Pi (hostapd) | 192.168.4.1 | 1st (most common) |
| Android Hotspot | 192.168.43.1 | 2nd |
| iOS Personal Hotspot | 172.20.10.1 | Not auto-detected* |
| Generic Router | 192.168.1.1 | 4th |

*iOS hotspots require device-specific configuration and are not auto-detected by the app.

## Troubleshooting

### App can't auto-detect PI IP
1. Verify you're connected to "MarketZone" WiFi
2. Manually check PI's IP:
   ```bash
   # On the PI:
   hostname -I
   ```
3. Manually enter the IP in the Connect dialog: `http://<PI_IP>:8765`

### WiFi connection fails
- **Android**: Ensure location permission is granted (required for WiFi scanning)
- **iOS**: Hotspot connections may require manual WiFi settings configuration
- **Windows**: Some enterprise networks block hotspot connections

### PI not accessible after WiFi connection
- Verify the PI's web server is running on port 8765:
  ```bash
  # On the PI:
  netstat -tulpn | grep 8765
  ```
- Check firewall settings on the PI
- Try pinging the gateway: `ping 192.168.4.1`

### Connection works but data doesn't load
- The mzSpecials app must be running on the PI
- Check PI logs for any API errors
- Verify the API port (8765) is not blocked

## Manual Configuration

If auto-detection fails, you can always manually configure:

1. Find the PI's IP address (on the PI run `hostname -I`)
2. Tap **"Connect"** button
3. Enter: `http://<PI_IP>:8765`
4. Tap **"Save"**

## Security Notes

- WiFi passwords are stored in platform secure storage (iOS Keychain, Android KeyStore, Windows Credential Manager)
- The default password is hardcoded but can be changed after first connection
- Consider changing "Sweet$Treats99" to a more secure password if needed
- The PI hotspot should use WPA2 encryption minimum

## Future Enhancements

Potential improvements for hotspot connectivity:
- mDNS/Bonjour discovery for automatic service location
- QR code scanning for easy configuration
- Multiple PI profiles for different locations
- Offline mode with local caching
