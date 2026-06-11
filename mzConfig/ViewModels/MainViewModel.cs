using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using mzConfigure.Models;
using mzConfigure.Services;

namespace mzConfigure.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SpecialsApiService _apiService;
    private readonly IDialogService _dialogService;
    private readonly IWiFiService _wifiService;
    private string _status = "Ready";
    private bool _isLoading;
    private string _raspberryPiUrl;
    private bool _isConnected;
    private string _headerText = "Rolling Pin Bakery";
    private string _headerColor = "#FFFFFF";
    private bool _isPortrait;
    private bool _isLoadingOrientation;

    public MainViewModel() : this(new DialogService(), new WiFiService())
    {
    }

    public MainViewModel(IDialogService dialogService, IWiFiService wifiService)
    {
        _dialogService = dialogService;
        _wifiService = wifiService;
        _apiService = new SpecialsApiService();

        // Load saved URL from preferences
        _raspberryPiUrl = Preferences.Get("RaspberryPiUrl", "http://raspberrypi.local:8765");
        _apiService.BaseUrl = _raspberryPiUrl;

        Specials = new ObservableCollection<Special>();

        LoadSpecialsCommand = new Command(async () => await LoadSpecials());
        ClearSpecialsCommand = new Command(async () => await ClearSpecials());
        UpdateSpecialsCommand = new Command(async () => await UpdateSpecials());
        AddSpecialCommand = new Command(AddSpecial);
        RemoveSpecialCommand = new Command<Special>(RemoveSpecial);
        TestConnectionCommand = new Command(async () => await TestConnection());
        PickColorCommand = new Command<Special>(async (special) => await PickColor(special));
        ConnectCommand = new Command(async () => await ShowConnectDialog());
        HeaderCommand = new Command(async () => await ShowHeaderDialog(), () => _isConnected);
        WiFiCommand = new Command(async () => await ShowWiFiDialog());

        // Auto-connect and load on startup
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Status = "Connecting...";
        IsLoading = true;

        try
        {
            var isConnected = await _apiService.TestConnectionAsync();
            IsConnected = isConnected;

            if (isConnected)
            {
                Status = "Connected. Loading specials...";
                await LoadSpecials();
            }
            else
            {
                Status = "Not connected. Use Connect button to configure.";
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            Status = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public ObservableCollection<Special> Specials { get; }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionInfo));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageTitleColor));
            OnPropertyChanged(nameof(ConnectButtonText));
            OnPropertyChanged(nameof(ConnectButtonColor));
            ((Command)HeaderCommand).ChangeCanExecute();
        }
    }

    public string RaspberryPiUrl
    {
        get => _raspberryPiUrl;
        set
        {
            _raspberryPiUrl = value;
            _apiService.BaseUrl = value;
            Preferences.Set("RaspberryPiUrl", value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionInfo));
        }
    }

    public string ConnectionInfo => _isConnected 
        ? $"Connected: {_raspberryPiUrl}" 
        : $"Not Connected: {_raspberryPiUrl}";

    public string HeaderText
    {
        get => _headerText;
        set
        {
            _headerText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string HeaderColor
    {
        get => _headerColor;
        set
        {
            _headerColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitleColor));
        }
    }

    public string PageTitle => _isConnected ? $"{_headerText} Config" : "MarketZone Config";

    public bool IsPortrait
    {
        get => _isPortrait;
        set
        {
            if (_isPortrait == value)
                return;

            _isPortrait = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OrientationText));

            if (_isConnected && !_isLoadingOrientation)
                _ = UpdateOrientation();
        }
    }

    public string OrientationText => _isPortrait ? "Portrait" : "Landscape";

    public Microsoft.Maui.Graphics.Color PageTitleColor
    {
        get
        {
            if (!_isConnected)
                return Microsoft.Maui.Graphics.Colors.Black;

            try
            {
                return Microsoft.Maui.Graphics.Color.FromArgb(_headerColor);
            }
            catch
            {
                return Microsoft.Maui.Graphics.Colors.Black;
            }
        }
    }

    public string ConnectButtonText => _isConnected ? "Disconnect" : "Connect";

    public Microsoft.Maui.Graphics.Color ConnectButtonColor => _isConnected 
        ? Microsoft.Maui.Graphics.Colors.Red 
        : Microsoft.Maui.Graphics.Colors.Green;

    public ICommand LoadSpecialsCommand { get; }
    public ICommand ClearSpecialsCommand { get; }
    public ICommand UpdateSpecialsCommand { get; }
    public ICommand AddSpecialCommand { get; }
    public ICommand RemoveSpecialCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand PickColorCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand HeaderCommand { get; }
    public ICommand WiFiCommand { get; }

    private async Task LoadSpecials()
    {
        IsLoading = true;
        Status = "Loading specials...";

        try
        {
            var specials = await _apiService.GetSpecialsAsync();
            Specials.Clear();
            foreach (var special in specials)
            {
                Specials.Add(special);
            }

            // Update header text and color to match the display when connected
            if (_isConnected)
            {
                try
                {
                    var header = await _apiService.GetHeaderAsync();
                    HeaderText = string.IsNullOrWhiteSpace(header.Text) ? "Rolling Pin Bakery" : header.Text;
                    HeaderColor = string.IsNullOrWhiteSpace(header.Color) ? "#FFFFFF" : header.Color;
                }
                catch
                {
                    // Fallback to defaults if header endpoint fails
                    HeaderText = "Rolling Pin Bakery";
                    HeaderColor = "#FFFFFF";
                }

                try
                {
                    var orientation = await _apiService.GetOrientationAsync();
                    _isLoadingOrientation = true;
                    IsPortrait = orientation == "portrait";
                    _isLoadingOrientation = false;
                }
                catch
                {
                    // Default orientation is landscape if the endpoint is unavailable.
                    _isLoadingOrientation = true;
                    IsPortrait = false;
                    _isLoadingOrientation = false;
                }
            }

            Status = $"Loaded {specials.Count} specials";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearSpecials()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirm", 
            "Clear all specials from the display?");
        
        if (!confirm)
            return;

        IsLoading = true;
        Status = "Clearing specials...";
        
        try
        {
            await _apiService.ClearSpecialsAsync();
            Specials.Clear();
            Status = "Specials cleared";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateSpecials()
    {
        if (Specials.Count == 0)
        {
            await _dialogService.ShowAlertAsync("Info", "No specials to update");
            return;
        }

        IsLoading = true;
        Status = "Updating specials...";
        
        try
        {
            var count = await _apiService.UpdateSpecialsAsync(Specials.ToList());
            Status = $"Updated {count} specials";
            await _dialogService.ShowAlertAsync("Success", $"Updated {count} specials");
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddSpecial()
    {
        Specials.Add(new Special 
        { 
            Text = $"Special {Specials.Count + 1}", 
            Color = "#FFFFFF" 
        });
        Status = "Special added (not yet sent to display)";
    }

    private void RemoveSpecial(Special special)
    {
        Specials.Remove(special);
        Status = "Special removed (not yet sent to display)";
    }

    private async Task PickColor(Special special)
    {
        try
        {
            // Simple action sheet for color selection
            var colors = new Dictionary<string, string>
            {
                { "White", "#FFFFFF" },
                { "Pink", "#FF1595" },
                { "Red", "#FF0000" },
                { "Orange", "#FF8C00" },
                { "Yellow", "#FFFF00" },
                { "Lime", "#00FF00" },
                { "Cyan", "#00FFFF" },
                { "Blue", "#0000FF" },
                { "Purple", "#800080" },
                { "Magenta", "#FF00FF" }
            };

            var colorNames = colors.Keys.ToArray();
            var action = await Application.Current.MainPage.DisplayActionSheet(
                "Select Color", 
                "Cancel", 
                null, 
                colorNames);

            if (!string.IsNullOrEmpty(action) && action != "Cancel" && colors.ContainsKey(action))
            {
                special.Color = colors[action];
                Status = $"Color changed to {action}";
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to select color: {ex.Message}");
        }
    }

    private async Task TestConnection()
    {
        IsLoading = true;
        Status = "Testing connection...";

        try
        {
            var isConnected = await _apiService.TestConnectionAsync();
            if (isConnected)
            {
                Status = "Connected successfully!";
                await _dialogService.ShowAlertAsync("Success", "Connected to Raspberry Pi");
            }
            else
            {
                Status = "Connection failed";
                await _dialogService.ShowAlertAsync("Error", "Could not connect to Raspberry Pi");
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowConnectDialog()
    {
        // If already connected, disconnect
        if (_isConnected)
        {
            IsConnected = false;
            Status = "Disconnected";
            Specials.Clear();
            HeaderText = "Rolling Pin Bakery";
            HeaderColor = "#FFFFFF";
            _isLoadingOrientation = true;
            IsPortrait = false;
            _isLoadingOrientation = false;
            return;
        }

        // Show connect dialog
        try
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                "Connect to Raspberry Pi",
                "Enter the URL for your Raspberry Pi:",
                initialValue: _raspberryPiUrl,
                placeholder: "http://raspberrypi.local:8765",
                accept: "Save",
                cancel: "Cancel",
                keyboard: Keyboard.Url);

            if (!string.IsNullOrWhiteSpace(result))
            {
                RaspberryPiUrl = result;
                Status = "Connecting...";
                IsLoading = true;

                try
                {
                    var isConnected = await _apiService.TestConnectionAsync();
                    IsConnected = isConnected;

                    if (isConnected)
                    {
                        Status = "Connected. Loading specials...";
                        await LoadSpecials();
                    }
                    else
                    {
                        Status = "Connection failed. Check URL and try again.";
                        await _dialogService.ShowAlertAsync("Connection Failed", "Could not connect to the Raspberry Pi. Please check the URL and try again.");
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    Status = $"Error: {ex.Message}";
                    await _dialogService.ShowAlertAsync("Error", ex.Message);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to show connection dialog: {ex.Message}");
        }
    }

    private async Task UpdateOrientation()
    {
        IsLoading = true;
        var orientation = _isPortrait ? "portrait" : "landscape";
        Status = $"Setting orientation to {OrientationText}...";

        try
        {
            await _apiService.SetOrientationAsync(orientation);
            Status = $"Orientation set to {OrientationText}";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);

            _isLoadingOrientation = true;
            _isPortrait = !_isPortrait;
            OnPropertyChanged(nameof(IsPortrait));
            OnPropertyChanged(nameof(OrientationText));
            _isLoadingOrientation = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowHeaderDialog()
    {
        if (!_isConnected)
            return;

        try
        {
            IsLoading = true;
            Status = "Loading header...";

            // Get current header from PI
            var currentHeader = await _apiService.GetHeaderAsync();

            // Prompt for new header text
            var newText = await Application.Current.MainPage.DisplayPromptAsync(
                "Edit Header",
                "Enter the header text:",
                initialValue: currentHeader.Text,
                placeholder: "Rolling Pin Bakery",
                accept: "Save",
                cancel: "Cancel");

            if (!string.IsNullOrWhiteSpace(newText))
            {
                Status = "Updating header...";

                // Keep the existing color, update text only
                await _apiService.SetHeaderAsync(newText, currentHeader.Color);

                // Update local state
                HeaderText = newText;
                Status = "Header updated successfully";
            }
            else
            {
                Status = "Header update cancelled";
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", $"Failed to update header: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowWiFiDialog()
    {
        try
        {
            // Get current WiFi SSID if available
            var currentSsid = await _wifiService.GetCurrentSsidAsync();
            var currentInfo = string.IsNullOrEmpty(currentSsid) 
                ? "Not connected to WiFi" 
                : $"Currently connected to: {currentSsid}";

            // Get PI WAP SSID from preferences or use default
            var defaultSsid = Preferences.Get("PiWapSsid", "MarketZone");
            var defaultPassword = "Sweet$Treats99";

            // Try to get stored password
            string? storedPassword = null;
            try
            {
                storedPassword = await SecureStorage.GetAsync($"PiWapPassword_{defaultSsid}");
            }
            catch
            {
                // SecureStorage may fail on some platforms
            }

            // If no stored password, use the default
            if (string.IsNullOrEmpty(storedPassword))
            {
                storedPassword = defaultPassword;

                // Save default password to secure storage
                try
                {
                    await SecureStorage.SetAsync($"PiWapPassword_{defaultSsid}", defaultPassword);
                }
                catch
                {
                    // SecureStorage may fail on some platforms
                }
            }

            // If we have both SSID and password stored, connect directly
            if (!string.IsNullOrEmpty(defaultSsid) && !string.IsNullOrEmpty(storedPassword))
            {
                // Ask user if they want to use stored credentials
                var useStored = await Application.Current.MainPage.DisplayAlert(
                    "Connect to PI WiFi",
                    $"{currentInfo}\n\nConnect to saved network '{defaultSsid}'?",
                    "Connect",
                    "Enter New");

                if (useStored)
                {
                    Status = $"Connecting to WiFi '{defaultSsid}'...";
                    IsLoading = true;

                    var connected = await _wifiService.ConnectToNetworkAsync(defaultSsid, storedPassword);

                    if (connected)
                    {
                        Status = $"Connected to WiFi '{defaultSsid}'";

                        // When connected to PI hotspot, use hotspot gateway IP
                        await TrySwitchToHotspotIp(defaultSsid);

                        await _dialogService.ShowAlertAsync("Success", 
                            $"Successfully connected to '{defaultSsid}'. You can now connect to the PI.", "OK");
                    }
                    else
                    {
                        Status = "WiFi connection failed";
                    }

                    IsLoading = false;
                    return;
                }
            }

            // Prompt for SSID
            var ssid = await Application.Current.MainPage.DisplayPromptAsync(
                "Connect to PI WiFi",
                $"{currentInfo}\n\nEnter the PI's WiFi network name (SSID):",
                initialValue: defaultSsid,
                placeholder: "MarketZone",
                accept: "Next",
                cancel: "Cancel");

            if (string.IsNullOrWhiteSpace(ssid))
                return;

            // Save SSID for next time
            Preferences.Set("PiWapSsid", ssid);

            // Try to get stored password for this SSID
            string? password = null;
            try
            {
                password = await SecureStorage.GetAsync($"PiWapPassword_{ssid}");
            }
            catch
            {
                // SecureStorage may fail on some platforms
            }

            // If no stored password, prompt for it
            if (string.IsNullOrEmpty(password))
            {
                password = await Application.Current.MainPage.DisplayPromptAsync(
                    "WiFi Password",
                    $"Enter the password for '{ssid}':",
                    placeholder: "Password (leave empty for open network)",
                    accept: "Connect",
                    cancel: "Cancel");

                if (password == null) // User cancelled
                    return;

                // Save password securely for next time
                if (!string.IsNullOrEmpty(password))
                {
                    try
                    {
                        await SecureStorage.SetAsync($"PiWapPassword_{ssid}", password);
                    }
                    catch
                    {
                        // SecureStorage may fail on some platforms
                    }
                }
            }

            Status = $"Connecting to WiFi '{ssid}'...";
            IsLoading = true;

            // Attempt to connect
            var success = await _wifiService.ConnectToNetworkAsync(ssid, password ?? "");

            if (success)
            {
                Status = $"Connected to WiFi '{ssid}'";

                // When connected to PI hotspot, use hotspot gateway IP
                await TrySwitchToHotspotIp(ssid);

                await _dialogService.ShowAlertAsync("Success", 
                    $"Successfully connected to '{ssid}'. You can now connect to the PI.", "OK");
            }
            else
            {
                Status = "WiFi connection failed";
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", $"WiFi connection error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// When connected to PI's hotspot, automatically switch to common hotspot gateway IPs
    /// </summary>
    private async Task TrySwitchToHotspotIp(string ssid)
    {
        // Only auto-switch for MarketZone hotspot
        if (!ssid.Equals("MarketZone", StringComparison.OrdinalIgnoreCase))
            return;

        // Common hotspot gateway IPs to try (in order of likelihood)
        var hotspotIps = new[]
        {
            "192.168.4.1",      // Most common for Linux/Raspberry Pi hotspots
            "192.168.43.1",     // Android hotspot default
            "10.0.0.1",         // Some routers/hotspots
            "192.168.1.1"       // Generic router default
        };

        string? workingIp = null;

        // Try each IP quickly
        foreach (var ip in hotspotIps)
        {
            try
            {
                var testUrl = $"http://{ip}:8765";
                var tempService = new SpecialsApiService { BaseUrl = testUrl };

                // Quick connection test with 2 second timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var canConnect = await tempService.TestConnectionAsync();

                if (canConnect)
                {
                    workingIp = ip;
                    break;
                }
            }
            catch
            {
                // Try next IP
                continue;
            }
        }

        // If we found a working IP, switch to it
        if (workingIp != null)
        {
            var newUrl = $"http://{workingIp}:8765";
            RaspberryPiUrl = newUrl;
            Status = $"Auto-detected PI at {workingIp}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
