using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using mzConfigure.Models;
using mzConfigure.Services;

namespace mzConfigure.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SpecialsApiService _apiService;
    private readonly IDialogService _dialogService;
    private readonly IWiFiService _wifiService;
    private readonly SpecialsLibraryService _libraryService;
    private string _status = "Ready";
    private bool _isLoading;
    private string _raspberryPiUrl;
    private bool _isConnected;
    private string _headerText = "Rolling Pin Bakery";
    private string _headerColor = "#FFFFFF";
    private bool _isPortrait;
    private bool _isLoadingOrientation;
    private bool _showMoreOptions;

    public MainViewModel() : this(new DialogService(), new WiFiService())
    {
    }

    public MainViewModel(IDialogService dialogService, IWiFiService wifiService)
    {
        Log.Separator("MainViewModel: Initializing");
        _dialogService = dialogService;
        _wifiService = wifiService;
        _apiService = new SpecialsApiService();
        _libraryService = new SpecialsLibraryService();

        // Load saved URL from preferences
        _raspberryPiUrl = Preferences.Get("RaspberryPiUrl", "http://10.42.0.1:8765");
        _apiService.BaseUrl = _raspberryPiUrl;
        Log.Info($"Loaded saved URL: {_raspberryPiUrl}");

        Specials = new ObservableCollection<Special>();

        LoadSpecialsCommand = new Command(async () => await LoadSpecials());
        ClearSpecialsCommand = new Command(async () => await ClearSpecials());
        UpdateSpecialsCommand = new Command(async () => await UpdateSpecials());
        AddSpecialCommand = new Command(async () => await AddSpecial());
        RemoveSpecialCommand = new Command<Special>(RemoveSpecial);
        TestConnectionCommand = new Command(async () => await TestConnection());
        PickColorCommand = new Command<Special>(async (special) => await PickColor(special));
        ConnectCommand = new Command(async () => await ShowConnectDialog());
        HeaderCommand = new Command(async () => await ShowHeaderDialog(), () => _isConnected);
        WiFiCommand = new Command(async () => await ShowWiFiDialog());
        LoadFromLibraryCommand = new Command(async () => await LoadFromLibrary());
        MoveUpCommand = new Command<Special>(MoveUp);
        MoveDownCommand = new Command<Special>(MoveDown);
        ToggleMoreOptionsCommand = new Command(ToggleMoreOptions);
        TestAnimationCommand = new Command(async () => await TestAnimation());
        DiagnosticCommand = new Command(async () => await ShowDiagnostics());
        ToggleOrientationCommand = new Command(async () => await ToggleOrientation());

        // Auto-connect and load on startup
        _ = InitializeAsync();
    }

    private Page? GetCurrentPage()
    {
        return Application.Current?.Windows?.FirstOrDefault()?.Page;
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

    public string PageTitle => _isConnected ? $"{_headerText} [Config]" : "MarketZone [Config]";

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

            // Don't auto-update during loading or manual toggle (ToggleOrientation handles API call)
            // Only auto-update if this is being set from external source (like initial load)
        }
    }

    public string OrientationText => _isPortrait ? "Land" : "Port";

    public bool ShowMoreOptions
    {
        get => _showMoreOptions;
        set
        {
            _showMoreOptions = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowMoreButtonText));
        }
    }

    public string ShowMoreButtonText => _showMoreOptions ? "▲" : "▼";

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
    public ICommand LoadFromLibraryCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ToggleMoreOptionsCommand { get; }
    public ICommand TestAnimationCommand { get; }
    public ICommand DiagnosticCommand { get; }
    public ICommand ToggleOrientationCommand { get; }

    private async Task LoadSpecials()
    {
        Log.Separator("LoadSpecials: Starting");
        IsLoading = true;
        Status = "Loading specials...";

        try
        {
            Log.Info($"Fetching specials from {_raspberryPiUrl}");
            var specials = await _apiService.GetSpecialsAsync();
            Log.Info($"Received {specials.Count} specials from API");

            Specials.Clear();
            foreach (var special in specials)
            {
                Log.Debug($"  - Special: '{special.Text}' (Color: {special.Color})");
                Specials.Add(special);
            }

            // Keep local library persistent and in sync with what exists on the display.
            await _libraryService.AddToLibraryAsync(specials);
            Log.Info($"Updated library with {specials.Count} specials");

            // Update header text and color to match the display when connected
            if (_isConnected)
            {
                Log.Info("Fetching header information...");
                try
                {
                    var header = await _apiService.GetHeaderAsync();
                    HeaderText = string.IsNullOrWhiteSpace(header.Text) ? "Rolling Pin Bakery" : header.Text;
                    HeaderColor = string.IsNullOrWhiteSpace(header.Color) ? "#FFFFFF" : header.Color;
                    Log.Info($"Header loaded - Text: '{HeaderText}', Color: {HeaderColor}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to load header - {ex.Message}");
                    // Fallback to defaults if header endpoint fails
                    HeaderText = "Rolling Pin Bakery";
                    HeaderColor = "#FFFFFF";
                }

                Log.Info("Fetching orientation...");
                try
                {
                    var orientation = await _apiService.GetOrientationAsync();
                    _isLoadingOrientation = true;
                    _isPortrait = orientation == "portrait";
                    OnPropertyChanged(nameof(IsPortrait));
                    OnPropertyChanged(nameof(OrientationText));
                    _isLoadingOrientation = false;
                    Log.Info($"Orientation loaded - {orientation} (IsPortrait: {_isPortrait}, Button shows: {OrientationText})");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to load orientation - {ex.Message}, defaulting to landscape");
                    // Default orientation is landscape if the endpoint is unavailable.
                    _isLoadingOrientation = true;
                    _isPortrait = false;
                    OnPropertyChanged(nameof(IsPortrait));
                    OnPropertyChanged(nameof(OrientationText));
                    _isLoadingOrientation = false;
                }
            }

            Status = $"Loaded {specials.Count} specials";
            Log.Separator($"LoadSpecials: Completed successfully - {specials.Count} specials");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "LoadSpecials: FAILED");
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
        Log.Separator("UpdateSpecials: Starting");
        if (Specials.Count == 0)
        {
            Log.Info("No specials to update");
            await _dialogService.ShowAlertAsync("Info", "No specials to update");
            return;
        }

        IsLoading = true;
        Status = "Updating specials...";
        Log.Info($"Sending {Specials.Count} specials to PI");
        foreach (var special in Specials)
        {
            Log.Debug($"  - '{special.Text}' (Color: {special.Color})");
        }

        try
        {
            var count = await _apiService.UpdateSpecialsAsync(Specials.ToList());
            Log.Info($"API confirmed {count} specials updated");

            // Save to library (will filter duplicates automatically)
            await _libraryService.AddToLibraryAsync(Specials.ToList());
            Log.Info($"Library updated with {Specials.Count} specials");

            Status = $"Updated {count} specials";
            Log.Separator("UpdateSpecials: Success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "UpdateSpecials: FAILED");
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddSpecial()
    {
        try
        {
            var currentPage = GetCurrentPage();
            if (currentPage == null) return;

            // Show combined add dialog with existing specials to filter library
            var addDialog = new Views.AddSpecialDialog(Specials.ToList());
            await currentPage.Navigation.PushModalAsync(addDialog);

            // Wait for user to complete or cancel
            await Task.Run(async () =>
            {
                while (currentPage.Navigation.ModalStack.Contains(addDialog))
                {
                    await Task.Delay(100);
                }
            });

            if (!string.IsNullOrEmpty(addDialog.SpecialText) && !string.IsNullOrEmpty(addDialog.SpecialColorHex))
            {
                // Check if already exists in current list (avoid duplicates)
                var exists = Specials.Any(s =>
                    s.Text.Trim().Equals(addDialog.SpecialText.Trim(), StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    var newSpecial = new Special
                    {
                        Text = addDialog.SpecialText,
                        Color = addDialog.SpecialColorHex
                    };

                    Specials.Add(newSpecial);
                    Status = $"Added '{addDialog.SpecialText}' with {addDialog.SpecialColorName} color";

                    // Add to library for future use
                    await _libraryService.AddToLibraryAsync(new[] { newSpecial });
                }
                else
                {
                    await _dialogService.ShowAlertAsync(
                        "Duplicate",
                        $"'{addDialog.SpecialText}' is already in the list.");
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to add special: {ex.Message}");
        }
    }

    private void RemoveSpecial(Special special)
    {
        Specials.Remove(special);
        Status = "Special removed (not yet sent to display)";
    }

    private void MoveUp(Special special)
    {
        var index = Specials.IndexOf(special);
        if (index > 0)
        {
            Specials.Move(index, index - 1);
            Status = "Special moved up (not yet sent to display)";
        }
    }

    private void MoveDown(Special special)
    {
        var index = Specials.IndexOf(special);
        if (index < Specials.Count - 1)
        {
            Specials.Move(index, index + 1);
            Status = "Special moved down (not yet sent to display)";
        }
    }

    private async Task PickColor(Special special)
    {
        try
        {
            var currentPage = GetCurrentPage();
            if (currentPage == null) return;

            // Show visual color picker
            var colorPicker = new Views.ColorPickerDialog();
            await currentPage.Navigation.PushModalAsync(colorPicker);

            // Wait for user to select or cancel
            await Task.Run(async () =>
            {
                while (currentPage.Navigation.ModalStack.Contains(colorPicker))
                {
                    await Task.Delay(100);
                }
            });

            if (!string.IsNullOrEmpty(colorPicker.SelectedColorHex))
            {
                special.Color = colorPicker.SelectedColorHex;
                Status = $"Color changed to {colorPicker.SelectedColorName}";
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to select color: {ex.Message}");
        }
    }

    private async Task LoadFromLibrary()
    {
        try
        {
            // Navigate to library selection page
            var libraryPage = new Views.LibrarySelectionPage();

            // Set up callback to receive selected items
            libraryPage.OnSpecialsSelected = (selectedSpecials) =>
            {
                // Add selected items to current specials list
                foreach (var special in selectedSpecials)
                {
                    // Check if already exists in current list (avoid duplicates)
                    var exists = Specials.Any(s => 
                        s.Text.Trim().Equals(special.Text.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        Specials.Add(special);
                    }
                }

                Status = $"Added {selectedSpecials.Count} specials from library (not yet sent to display)";
            };

            await Shell.Current.Navigation.PushAsync(libraryPage);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to open library: {ex.Message}");
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
            Log.Separator("ShowConnectDialog: Disconnecting");
            IsConnected = false;
            Status = "Disconnected";
            Specials.Clear();
            HeaderText = "Rolling Pin Bakery";
            HeaderColor = "#FFFFFF";
            _isLoadingOrientation = true;
            IsPortrait = false;
            _isLoadingOrientation = false;
            Log.Info("Disconnected and cleared all data");
            return;
        }

        // Show connect dialog
        try
        {
            Log.Separator("ShowConnectDialog: Prompting for connection");
            var currentPage = GetCurrentPage();
            if (currentPage == null) return;

            var result = await currentPage.DisplayPromptAsync(
                "Connect to Raspberry Pi",
                "Enter the URL for your Raspberry Pi:",
                initialValue: _raspberryPiUrl,
                placeholder: "http://10.42.0.1:8765",
                accept: "Save",
                cancel: "Cancel",
                keyboard: Keyboard.Url);

            if (!string.IsNullOrWhiteSpace(result))
            {
                Log.Info($"User entered URL: {result}");
                // Process the input to ensure proper format
                var processedUrl = ProcessConnectionUrl(result);
                Log.Info($"Processed URL: {processedUrl}");

                RaspberryPiUrl = processedUrl;
                Status = "Connecting...";
                IsLoading = true;

                try
                {
                    Log.Info($"Testing connection to {processedUrl}...");
                    var isConnected = await _apiService.TestConnectionAsync();
                    IsConnected = isConnected;
                    Log.Info($"Connection test result: {isConnected}");

                    if (isConnected)
                    {
                        Log.Info("Connection successful, loading data from PI...");
                        Status = "Connected. Loading specials...";
                        await LoadSpecials();
                        Log.Separator("ShowConnectDialog: Connection complete");
                    }
                    else
                    {
                        Log.Warning("Connection test returned false");
                        Status = "Connection failed. Check URL and try again.";
                        await _dialogService.ShowAlertAsync("Connection Failed", "Could not connect to the Raspberry Pi. Please check the URL and try again.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "ShowConnectDialog: Connection FAILED");
                    IsConnected = false;
                    Status = $"Error: {ex.Message}";
                    await _dialogService.ShowAlertAsync("Error", ex.Message);
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                Log.Info("User cancelled connection dialog");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ShowConnectDialog: Dialog ERROR");
            await _dialogService.ShowAlertAsync("Error", $"Failed to show connection dialog: {ex.Message}");
        }
    }

    private string ProcessConnectionUrl(string input)
    {
        // Remove any whitespace
        input = input.Trim();

        // If it's already a complete URL with protocol and port, return as is
        if (input.StartsWith("http://") || input.StartsWith("https://"))
        {
            // Check if it has a port
            var uri = new Uri(input);
            if (uri.Port == 80 || uri.Port == 443) // Default HTTP/HTTPS ports
            {
                // Add explicit port 8765
                return $"{uri.Scheme}://{uri.Host}:8765{uri.PathAndQuery}";
            }
            return input;
        }

        // If it looks like just an IP address (e.g., "10.42.0.1" or "192.168.1.1")
        if (System.Net.IPAddress.TryParse(input, out _))
        {
            return $"http://{input}:8765";
        }

        // If it has a port but no protocol (e.g., "10.42.0.1:8765")
        if (input.Contains(':') && !input.Contains("://"))
        {
            return $"http://{input}";
        }

        // If it's a hostname without protocol (e.g., "raspberrypi.local")
        if (!input.Contains(':'))
        {
            return $"http://{input}:8765";
        }

        // Default: assume it's a hostname with port
        return $"http://{input}";
    }

    private async Task ToggleOrientation()
    {
        Log.Separator("ToggleOrientation: Starting");
        if (!_isConnected)
        {
            Log.Info("Not connected, showing alert");
            await _dialogService.ShowAlertAsync("Not Connected", "Please connect to the Raspberry Pi first.");
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Toggling orientation...";

            // Toggle the orientation
            var newOrientation = !_isPortrait;
            var orientationText = newOrientation ? "portrait" : "landscape";
            Log.Info($"Current={(_isPortrait ? "portrait" : "landscape")}, New={orientationText}");

            // Send to API
            Log.Info($"Sending API call to set orientation to {orientationText}");
            await _apiService.SetOrientationAsync(orientationText);

            // Update local state only after successful API call
            IsPortrait = newOrientation;
            Status = $"Orientation set to {OrientationText}";
            Log.Separator($"ToggleOrientation: Success - Button now shows '{OrientationText}'");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ToggleOrientation: FAILED");
            Status = $"Error: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", $"Failed to toggle orientation: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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

            var currentPage = GetCurrentPage();
            if (currentPage == null) return;

            // Prompt for new header text
            var newText = await currentPage.DisplayPromptAsync(
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
            var currentPage = GetCurrentPage();
            if (currentPage == null) return;

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
                var useStored = await currentPage.DisplayAlertAsync(
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
            var ssid = await currentPage.DisplayPromptAsync(
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
                password = await currentPage.DisplayPromptAsync(
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

    private void ToggleMoreOptions()
    {
        ShowMoreOptions = !ShowMoreOptions;
    }

    private async Task TestAnimation()
    {
        Log.Separator("TestAnimation: Starting");
        if (!_isConnected)
        {
            Log.Info("Not connected, showing alert");
            await _dialogService.ShowAlertAsync("Not Connected", "Please connect to the Raspberry Pi first.");
            return;
        }

        Status = "Triggering animation...";
        IsLoading = true;

        try
        {
            Log.Info($"Sending POST request to {_raspberryPiUrl}/blanking/trigger");
            await _apiService.TriggerAnimationAsync();
            Status = "Animation triggered successfully";
            Log.Separator("TestAnimation: Success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "TestAnimation: FAILED");
            Status = $"Animation test failed: {ex.Message}";
            await _dialogService.ShowAlertAsync("Error", $"Failed to trigger animation: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowDiagnostics()
    {
        Status = "Running diagnostics...";
        IsLoading = true;

        try
        {
            // Build diagnostic information
            var diagnosticInfo = new StringBuilder();
            diagnosticInfo.AppendLine("DIAGNOSTIC TEST SUMMARY:");
            diagnosticInfo.AppendLine("• DNS Test: Can the hostname be found?");
            diagnosticInfo.AppendLine("• Ping Test: Is the device responding?");
            diagnosticInfo.AppendLine("• TCP Test: Can we reach the port?");
            diagnosticInfo.AppendLine("• HTTP Test: Can the API respond?");
            diagnosticInfo.AppendLine("");

            diagnosticInfo.AppendLine("=== DEVICE INFO ===");
            diagnosticInfo.AppendLine($"Platform: {DeviceInfo.Platform}");
            diagnosticInfo.AppendLine($"Device Type: {DeviceInfo.DeviceType}");
            diagnosticInfo.AppendLine($"Model: {DeviceInfo.Model}");
            diagnosticInfo.AppendLine($"Version: {DeviceInfo.VersionString}");

            // Get current device IP address (excluding loopback)
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName);
                var ipv4Address = addresses.FirstOrDefault(a => 
                    a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !System.Net.IPAddress.IsLoopback(a));

                if (ipv4Address != null)
                {
                    diagnosticInfo.AppendLine($"Device IP: {ipv4Address}");
                }
                else
                {
                    diagnosticInfo.AppendLine("Device IP: Not available");
                }
            }
            catch
            {
                diagnosticInfo.AppendLine("Device IP: Unable to determine");
            }

            diagnosticInfo.AppendLine("\n=== APP STATUS ===");
            diagnosticInfo.AppendLine($"Connection Status: {(_isConnected ? "Connected" : "Disconnected")}");
            diagnosticInfo.AppendLine($"Target URL: {_raspberryPiUrl}");
            diagnosticInfo.AppendLine($"Specials Count: {Specials.Count}");
            diagnosticInfo.AppendLine($"Header: {_headerText}");
            diagnosticInfo.AppendLine($"Orientation: {OrientationText}");

            // Extract host and port from URL
            var uri = new Uri(_raspberryPiUrl);
            var host = uri.Host;
            var port = uri.Port;

            diagnosticInfo.AppendLine("\n=== NETWORK CONNECTIVITY ===");

            // Check network access
            var networkAccess = Connectivity.NetworkAccess;
            diagnosticInfo.AppendLine($"Network Access: {networkAccess}");

            if (networkAccess == NetworkAccess.Internet || networkAccess == NetworkAccess.Local)
            {
                diagnosticInfo.AppendLine("✓ Network available");

                // Check active connections
                var profiles = Connectivity.ConnectionProfiles;
                diagnosticInfo.AppendLine($"Connection Types: {string.Join(", ", profiles)}");

                diagnosticInfo.AppendLine("\n=== DNS RESOLUTION TEST ===");
                diagnosticInfo.AppendLine("(Tests if the hostname can be resolved to an IP address)");

                // Attempt to resolve DNS
                string resolvedIp = null;
                bool dnsFailed = false;
                try
                {
                    var addresses = await System.Net.Dns.GetHostAddressesAsync(host);
                    if (addresses.Length > 0)
                    {
                        resolvedIp = addresses[0].ToString();
                        diagnosticInfo.AppendLine($"✓ DNS Resolution: {host} -> {resolvedIp}");
                        diagnosticInfo.AppendLine($"  Host is resolvable");
                    }
                }
                catch (Exception ex)
                {
                    dnsFailed = true;
                    diagnosticInfo.AppendLine($"✗ DNS Resolution Failed: {ex.Message}");
                    diagnosticInfo.AppendLine($"  Cannot resolve hostname '{host}'");
                    diagnosticInfo.AppendLine($"  Check: Is the hostname correct? Is DNS working?");
                    diagnosticInfo.AppendLine("\n⚠ ABORTING: Cannot proceed without DNS resolution");

                    Status = "Diagnostics complete - DNS failed";
                    await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                    return;
                }

                // ICMP Ping Test
                diagnosticInfo.AppendLine("\n=== ICMP PING TEST ===");
                diagnosticInfo.AppendLine("(Tests if the host responds to ping packets)");

                bool pingFailed = false;
                if (resolvedIp != null)
                {
                    try
                    {
                        using var ping = new System.Net.NetworkInformation.Ping();
                        var pingReply = await ping.SendPingAsync(resolvedIp, 5000); // 5 second timeout

                        if (pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
                        {
                            diagnosticInfo.AppendLine($"✓ Ping Reply: {resolvedIp} responded in {pingReply.RoundtripTime}ms");
                            diagnosticInfo.AppendLine($"  Host is reachable on the network");
                            diagnosticInfo.AppendLine($"  TTL: {pingReply.Options?.Ttl}");
                        }
                        else
                        {
                            pingFailed = true;
                            diagnosticInfo.AppendLine($"✗ Ping Failed: {pingReply.Status}");
                            diagnosticInfo.AppendLine($"  Host at {resolvedIp} did not respond to ping");

                            if (pingReply.Status == System.Net.NetworkInformation.IPStatus.TimedOut)
                            {
                                diagnosticInfo.AppendLine($"  Request timed out after 5 seconds");
                                diagnosticInfo.AppendLine($"  Check: Is device on? Firewall blocking ICMP? Same network?");
                            }
                            else if (pingReply.Status == System.Net.NetworkInformation.IPStatus.DestinationHostUnreachable)
                            {
                                diagnosticInfo.AppendLine($"  Network route to host not found");
                                diagnosticInfo.AppendLine($"  Check: Same subnet? Network configuration?");
                            }
                            else
                            {
                                diagnosticInfo.AppendLine($"  Check: Firewall settings, network connectivity");
                            }

                            diagnosticInfo.AppendLine("\n⚠ ABORTING: Host not reachable, skipping remaining tests");

                            Status = "Diagnostics complete - Ping failed";
                            await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        pingFailed = true;
                        diagnosticInfo.AppendLine($"✗ Ping Error: {ex.Message}");
                        diagnosticInfo.AppendLine($"  Note: Some networks/devices may block ICMP ping");
                        diagnosticInfo.AppendLine($"  Continuing to TCP test...");
                    }
                }
                else
                {
                    diagnosticInfo.AppendLine("⊘ Ping Skipped: DNS resolution failed, no IP to ping");
                }

                // Test basic TCP connectivity
                diagnosticInfo.AppendLine("\n=== TCP CONNECTIVITY TEST ===");
                diagnosticInfo.AppendLine($"(Tests if port {port} is reachable and accepting connections)");
                var tcpSuccess = false;
                try
                {
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    var connectTask = tcpClient.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(5000);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                    {
                        sw.Stop();
                        if (tcpClient.Connected)
                        {
                            tcpSuccess = true;
                            diagnosticInfo.AppendLine($"✓ TCP Connection: {host}:{port} - Success ({sw.ElapsedMilliseconds}ms)");
                            diagnosticInfo.AppendLine($"  Port {port} is open and accepting connections");
                            if (resolvedIp != null)
                                diagnosticInfo.AppendLine($"  Connected to: {resolvedIp}");
                            tcpClient.Close();
                        }
                        else
                        {
                            diagnosticInfo.AppendLine($"✗ TCP Connection: {host}:{port} - Failed");
                            diagnosticInfo.AppendLine($"  Port {port} refused connection");
                            diagnosticInfo.AppendLine($"  Check: Is the service running? Is the port correct?");
                        }
                    }
                    else
                    {
                        diagnosticInfo.AppendLine($"✗ TCP Connection: {host}:{port} - Timeout (5s)");
                        diagnosticInfo.AppendLine($"  No response from {host}:{port}");
                        if (resolvedIp != null)
                        {
                            diagnosticInfo.AppendLine($"  Host resolved to {resolvedIp} but not responding");
                            diagnosticInfo.AppendLine($"  Check: Is the device powered on? Is it on the same network?");
                        }
                        else
                        {
                            diagnosticInfo.AppendLine($"  Check: Is the hostname/IP correct? Is the device reachable?");
                        }

                        diagnosticInfo.AppendLine("\n⚠ ABORTING: TCP port not reachable, skipping HTTP tests");

                        Status = "Diagnostics complete - TCP failed";
                        await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                        return;
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    diagnosticInfo.AppendLine($"✗ TCP Connection Failed: {ex.SocketErrorCode}");
                    diagnosticInfo.AppendLine($"  Error: {ex.Message}");

                    if (ex.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable)
                    {
                        diagnosticInfo.AppendLine($"  Host {host} is not reachable");
                        diagnosticInfo.AppendLine($"  Check: Is the device on? Same network? Firewall blocking?");
                    }
                    else if (ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                    {
                        diagnosticInfo.AppendLine($"  Connection refused - port {port} is closed");
                        diagnosticInfo.AppendLine($"  Check: Is the service running on port {port}?");
                    }
                    else if (ex.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    {
                        diagnosticInfo.AppendLine($"  Connection timed out");
                        diagnosticInfo.AppendLine($"  Check: Network connection, firewall, device status");
                    }

                    diagnosticInfo.AppendLine("\n⚠ ABORTING: TCP connection failed, skipping HTTP tests");

                    Status = "Diagnostics complete - TCP failed";
                    await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    diagnosticInfo.AppendLine($"✗ TCP Connection Error: {ex.GetType().Name}");
                    diagnosticInfo.AppendLine($"  {ex.Message}");

                    diagnosticInfo.AppendLine("\n⚠ ABORTING: TCP connection error, skipping HTTP tests");

                    Status = "Diagnostics complete - TCP failed";
                    await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                    return;
                }

                // Only run HTTP tests if TCP succeeded
                if (!tcpSuccess)
                {
                    diagnosticInfo.AppendLine("\n⚠ Skipping HTTP tests - TCP connection failed");

                    Status = "Diagnostics complete - TCP failed";
                    await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                    return;
                }

                diagnosticInfo.AppendLine("\n=== HTTP ENDPOINT TESTS ===");

                // Test HTTP endpoints with appropriate methods
                var endpoints = new[]
                {
                    ("/specials", "GET", "GET Specials"),
                    ("/orientation", "GET", "GET Orientation"),
                    ("/header", "GET", "GET Header"),
                    ("/blanking/trigger", "POST", "POST Animation")
                };

                foreach (var (endpoint, method, description) in endpoints)
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        ApiAuthService.ApplyTokenHeader(client);

                        HttpResponseMessage response;
                        if (method == "POST")
                        {
                            response = await client.PostAsync($"{_raspberryPiUrl}{endpoint}", null);
                        }
                        else
                        {
                            response = await client.GetAsync($"{_raspberryPiUrl}{endpoint}");
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            diagnosticInfo.AppendLine($"✓ {description}: {(int)response.StatusCode} {response.StatusCode}");
                        }
                        else
                        {
                            diagnosticInfo.AppendLine($"✗ {description}: {(int)response.StatusCode} {response.StatusCode}");
                            diagnosticInfo.AppendLine($"  Endpoint {endpoint} returned error status");

                            diagnosticInfo.AppendLine("\n⚠ ABORTING: HTTP endpoint failed, skipping remaining tests");

                            Status = "Diagnostics complete - HTTP failed";
                            await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                            return;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        diagnosticInfo.AppendLine($"✗ {description}: Timeout");
                        diagnosticInfo.AppendLine($"  Endpoint {endpoint} did not respond within 5 seconds");

                        diagnosticInfo.AppendLine("\n⚠ ABORTING: HTTP endpoint timeout, skipping remaining tests");

                        Status = "Diagnostics complete - HTTP timeout";
                        await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                        return;
                    }
                    catch (Exception ex)
                    {
                        diagnosticInfo.AppendLine($"✗ {description}: {ex.Message}");
                        diagnosticInfo.AppendLine($"  Error accessing {endpoint}");

                        diagnosticInfo.AppendLine("\n⚠ ABORTING: HTTP endpoint error, skipping remaining tests");

                        Status = "Diagnostics complete - HTTP error";
                        await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                        return;
                    }
                }

                // Overall API health check
                diagnosticInfo.AppendLine("\n=== API HEALTH CHECK ===");
                try
                {
                    var canConnect = await _apiService.TestConnectionAsync();
                    diagnosticInfo.AppendLine($"API Test: {(canConnect ? "✓ Success" : "✗ Failed")}");

                    if (!canConnect)
                    {
                        diagnosticInfo.AppendLine("\n⚠ API health check failed");

                        Status = "Diagnostics complete - API health check failed";
                        await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    diagnosticInfo.AppendLine($"API Test: ✗ Error - {ex.Message}");

                    diagnosticInfo.AppendLine("\n⚠ ABORTING: API health check error");

                    Status = "Diagnostics complete - API health check error";
                    await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
                    return;
                }
            }
            else
            {
                diagnosticInfo.AppendLine("✗ No network connectivity");
            }

            Status = "Diagnostics complete";
            await _dialogService.ShowAlertAsync("Diagnostics Report", diagnosticInfo.ToString());
        }
        catch (Exception ex)
        {
            Status = $"Diagnostics failed: {ex.Message}";
            await _dialogService.ShowAlertAsync("Diagnostics Error", $"Failed to run diagnostics: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

