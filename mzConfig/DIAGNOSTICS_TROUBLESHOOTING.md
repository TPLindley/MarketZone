# "Diagnostics Pass But Can't Connect" - Troubleshooting Guide

## The Problem
Your network diagnostics show everything is working (ping succeeds, network is good), but when you try to actually connect to the Pi's web service, it fails.

## Why This Happens
**Ping tests basic network connectivity** - can your device reach the Pi's IP address?  
**HTTP connection requires the web service to be running** - is the Python server actually listening on port 8765?

Think of it like this:
- ✅ Ping = "Can I reach the house?" (YES)
- ❌ HTTP = "Is anyone home to answer the door?" (NO)

## Updated Diagnostics
The diagnostics now test **both**:
1. Basic network connectivity (ping/TCP)
2. **HTTP service on port 8765** (actual API endpoint)

## What You'll See Now

### Scenario 1: Network Good, Service Not Running
```
=== Network Diagnostics ===

Target Host: 10.42.0.1
Full URL: http://10.42.0.1:8765
Port: 8765

Network Status:
Local IP: 192.168.1.100
Network: WiFi

✓ Can reach 10.42.0.1
✓ Ping time: 25ms
⚠ HTTP service test failed on port 8765: Connection refused

=== TROUBLESHOOTING ===
❌ The device can reach the host but the
   HTTP service is not responding.

Check:
• Is the Python server running on the Pi?
• Is it listening on port 8765?
• Run on Pi: 'sudo systemctl status mzSpecials'
• Or check: 'ps aux | grep python'
• Try: 'curl http://10.42.0.1:8765/specials'
```

### Scenario 2: Everything Working
```
=== Network Diagnostics ===

Target Host: 10.42.0.1
Full URL: http://10.42.0.1:8765
Port: 8765

✓ Connected to WiFi
✓ Local IP: 192.168.1.100
✓ Can reach 10.42.0.1
✓ Ping time: 25ms
✓ HTTP service is responding (port 8765)
```

## Steps to Fix "Service Not Running"

### 1. Check if the Service is Running
SSH into your Raspberry Pi and run:
```bash
sudo systemctl status mzSpecials
```

**If it says "inactive (dead)" or "failed":**
```bash
# Start the service
sudo systemctl start mzSpecials

# Check status again
sudo systemctl status mzSpecials
```

### 2. Check if Python is Listening on Port 8765
```bash
# See what's listening on port 8765
sudo netstat -tulpn | grep 8765

# Or use lsof
sudo lsof -i :8765
```

**Should see something like:**
```
tcp   0  0.0.0.0:8765   0.0.0.0:*   LISTEN   1234/python
```

**If nothing is listening:**
- The service isn't running
- Or it's configured for a different port

### 3. Check Service Logs
```bash
# View recent logs
sudo journalctl -u mzSpecials -n 50

# Or follow logs in real-time
sudo journalctl -u mzSpecials -f
```

**Look for errors like:**
- `Address already in use` - Port 8765 is taken by another process
- `Permission denied` - Service doesn't have permission to bind to port
- `ModuleNotFoundError` - Python dependencies missing
- Service crashes immediately after starting

### 4. Manually Test the Server
```bash
# SSH to the Pi, then:
curl http://localhost:8765/specials
```

**Expected:** JSON response with specials list  
**If you get "Connection refused":** Server isn't running  
**If you get response:** Server works locally, might be firewall issue

### 5. Check Firewall Settings
```bash
# Check if firewall is blocking port 8765
sudo ufw status

# If port 8765 isn't allowed:
sudo ufw allow 8765/tcp
sudo ufw reload
```

### 6. Restart Everything (Nuclear Option)
```bash
# On the Pi:
sudo systemctl restart mzSpecials
sudo systemctl status mzSpecials

# Reboot if needed
sudo reboot
```

## Common Causes & Solutions

| Symptom | Cause | Solution |
|---------|-------|----------|
| "Connection refused" | Service not running | `sudo systemctl start mzSpecials` |
| "Connection timeout" | Wrong IP or firewall | Check IP, disable firewall temporarily |
| Service starts then stops | Crash on startup | Check logs: `journalctl -u mzSpecials` |
| "Port already in use" | Old instance running | `sudo killall python` then restart service |
| Works locally, not remotely | Firewall or binding to localhost | Ensure server binds to `0.0.0.0:8765` not `127.0.0.1:8765` |

## Verify Your Service Configuration

The Python server should be configured like this:

```python
# Make sure it binds to all interfaces (0.0.0.0), not just localhost
app.run(host='0.0.0.0', port=8765)
```

**NOT:**
```python
app.run(host='127.0.0.1', port=8765)  # ❌ Only localhost!
```

## Quick Diagnostic Commands (Run on Pi)

```bash
# All-in-one diagnostic
echo "=== Service Status ===" && \
sudo systemctl status mzSpecials && \
echo -e "\n=== Port 8765 ===" && \
sudo netstat -tulpn | grep 8765 && \
echo -e "\n=== Local Test ===" && \
curl -I http://localhost:8765/specials
```

## Next Steps

1. **Restart your app** to get the updated diagnostics
2. **Run diagnostics** - it will now test the HTTP service
3. **If HTTP test fails** - follow the steps above to check the Pi
4. **Try Test Connection** button - it now shows detailed error info

The enhanced diagnostics will tell you exactly where the problem is!
