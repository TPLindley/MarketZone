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
    private string _status = "Ready";
    private bool _isLoading;
    private string _raspberryPiUrl;

    public MainViewModel()
    {
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

    public string RaspberryPiUrl
    {
        get => _raspberryPiUrl;
        set
        {
            _raspberryPiUrl = value;
            _apiService.BaseUrl = value;
            Preferences.Set("RaspberryPiUrl", value);
            OnPropertyChanged();
        }
    }

    public ICommand LoadSpecialsCommand { get; }
    public ICommand ClearSpecialsCommand { get; }
    public ICommand UpdateSpecialsCommand { get; }
    public ICommand AddSpecialCommand { get; }
    public ICommand RemoveSpecialCommand { get; }
    public ICommand TestConnectionCommand { get; }

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
            Status = $"Loaded {specials.Count} specials";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearSpecials()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Confirm", 
            "Clear all specials from the display?", 
            "Yes", 
            "No");
        
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
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
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
            await Application.Current!.MainPage!.DisplayAlert("Info", "No specials to update", "OK");
            return;
        }

        IsLoading = true;
        Status = "Updating specials...";
        
        try
        {
            var count = await _apiService.UpdateSpecialsAsync(Specials.ToList());
            Status = $"Updated {count} specials";
            await Application.Current!.MainPage!.DisplayAlert("Success", $"Updated {count} specials", "OK");
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
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
                await Application.Current!.MainPage!.DisplayAlert("Success", "Connected to Raspberry Pi", "OK");
            }
            else
            {
                Status = "Connection failed";
                await Application.Current!.MainPage!.DisplayAlert("Error", "Could not connect to Raspberry Pi", "OK");
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
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
