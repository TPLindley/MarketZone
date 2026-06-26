using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace mzConfigure.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private string _raspberryPiUrl;
    private string _savedMessage = string.Empty;

    public SettingsViewModel()
    {
        // Load saved URL from preferences
        _raspberryPiUrl = Preferences.Get("RaspberryPiUrl", "http://raspberrypi.local:8765");
        
        SaveCommand = new Command(SaveSettings);
        ResetCommand = new Command(ResetSettings);
    }

    public string RaspberryPiUrl
    {
        get => _raspberryPiUrl;
        set
        {
            _raspberryPiUrl = value;
            OnPropertyChanged();
        }
    }

    public string SavedMessage
    {
        get => _savedMessage;
        set
        {
            _savedMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    private void SaveSettings()
    {
        Preferences.Set("RaspberryPiUrl", RaspberryPiUrl);
        SavedMessage = "Settings saved!";
        
        // Clear message after 2 seconds
        Task.Delay(2000).ContinueWith(_ =>
        {
            SavedMessage = string.Empty;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ResetSettings()
    {
        RaspberryPiUrl = "http://raspberrypi.local:8765";
        Preferences.Clear();
        SavedMessage = "Settings reset to defaults";
        
        // Clear message after 2 seconds
        Task.Delay(2000).ContinueWith(_ =>
        {
            SavedMessage = string.Empty;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
