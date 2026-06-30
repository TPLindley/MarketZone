using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using mzConfigure.Services;

namespace mzConfigure.ViewModels;

public class AboutViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _mainViewModel;
    private readonly SpecialsApiService _apiService;
    private readonly ServiceHealthMonitor _healthMonitor;
    private readonly IWiFiService _wifiService;
    private string _deviceIp = "Detecting...";

    public event PropertyChangedEventHandler? PropertyChanged;

    public AboutViewModel(
        MainViewModel mainViewModel,
        SpecialsApiService apiService,
        ServiceHealthMonitor healthMonitor,
        IWiFiService wifiService)
    {
        _mainViewModel = mainViewModel;
        _apiService = apiService;
        _healthMonitor = healthMonitor;
        _wifiService = wifiService;

        TestCommand = new Command(async () => await ExecuteTest(), () => IsConnected);
        DiagnosticsCommand = new Command(async () => await ExecuteDiagnostics());
        ClearLibraryCommand = new Command(async () => await ExecuteClearLibrary(), () => IsConnected);
        CloseCommand = new Command(async () => await ExecuteClose());

        _ = LoadDeviceIpAsync();
    }

    // Commands
    public ICommand TestCommand { get; }
    public ICommand DiagnosticsCommand { get; }
    public ICommand ClearLibraryCommand { get; }
    public ICommand CloseCommand { get; }

    // Application Properties
    public string AppVersionText => MarketZone.AppVersion.FullVersion;
    public string TargetUrl => _mainViewModel.RaspberryPiUrl;
    public bool IsConnected => _mainViewModel.IsConnected;

    public string ConnectionStatusText => IsConnected ? "Connected" : "Disconnected";
    public Color ConnectionStatusColor => IsConnected ? Colors.Green : Colors.Red;

    // Device Properties
    public string DevicePlatform => DeviceInfo.Platform.ToString();
    public string DeviceModel => DeviceInfo.Model;
    public string DeviceVersion => DeviceInfo.VersionString;
    public string DeviceIp
    {
        get => _deviceIp;
        private set
        {
            _deviceIp = value;
            OnPropertyChanged();
        }
    }

    // Service Properties
    public string ServiceVersion => 
        !string.IsNullOrEmpty(_apiService.LastServiceVersion) 
            ? _apiService.LastServiceVersion 
            : "Unknown";

    public string ServiceUptime
    {
        get
        {
            if (_apiService.LastServiceUptimeSeconds > 0)
            {
                return $"{_healthMonitor.ServiceUptimeText} ({_apiService.LastServiceUptimeSeconds}s)";
            }
            return "Unknown";
        }
    }

    // Health Monitor Properties
    public string MonitorStatus => _healthMonitor.IsMonitoring ? "Active" : "Inactive";
    public string MonitorInterval => $"{_healthMonitor.CheckIntervalMinutes:F1} minutes";
    public string LastCheckTime => 
        _healthMonitor.LastSuccessfulPing.HasValue 
            ? _healthMonitor.LastSuccessfulPing.Value.ToString("HH:mm:ss")
            : "Never";

    // Content Properties
    public string SpecialsCount => _mainViewModel.Specials.Count.ToString();
    public string HeaderText
    {
        get
        {
            var header = _mainViewModel.HeaderText;
            if (string.IsNullOrEmpty(header))
                return "None";
            return header.Length > 30 ? header.Substring(0, 30) + "..." : header;
        }
    }
    public string OrientationText => _mainViewModel.OrientationText;

    private async Task LoadDeviceIpAsync()
    {
        try
        {
            var ipAddress = await _wifiService.GetLocalIpAddressAsync();
            DeviceIp = ipAddress ?? "Not available";
        }
        catch
        {
            DeviceIp = "Unable to determine";
        }
    }

    private async Task ExecuteTest()
    {
        await Application.Current!.MainPage!.Navigation.PopModalAsync();
        // Call the test animation from main view model
        await _mainViewModel.ExecuteTestAnimation();
    }

    private async Task ExecuteDiagnostics()
    {
        await Application.Current!.MainPage!.Navigation.PopModalAsync();
        // Call diagnostics from main view model
        await _mainViewModel.ExecuteDiagnostics();
    }

    private async Task ExecuteClearLibrary()
    {
        try
        {
            // Confirm with user
            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Clear Library",
                "Are you sure you want to clear the server library? This will remove all past specials from the server.",
                "Yes, Clear",
                "Cancel");

            if (!confirm)
                return;

            // Call API to clear library
            await _apiService.ClearLibraryAsync();

            // Show success message
            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Server library has been cleared successfully.",
                "OK");

            Log.Info("AboutViewModel: Server library cleared by user");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "AboutViewModel: Failed to clear library");
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                $"Failed to clear library: {ex.Message}",
                "OK");
        }
    }

    private async Task ExecuteClose()
    {
        await Application.Current!.MainPage!.Navigation.PopModalAsync();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
