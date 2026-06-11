using System.Collections.ObjectModel;
using mzConfigure.Models;
using mzConfigure.Services;

namespace mzConfigure.Views;

public partial class LibrarySelectionPage : ContentPage
{
    private readonly SpecialsLibraryService _libraryService;
    private readonly ObservableCollection<SelectableSpecial> _selectableItems;

    public ObservableCollection<SelectableSpecial> SelectableItems => _selectableItems;

    // This will be set by the calling page to receive selected items
    public Action<List<Special>>? OnSpecialsSelected { get; set; }

    public LibrarySelectionPage()
    {
        InitializeComponent();
        _libraryService = new SpecialsLibraryService();
        _selectableItems = new ObservableCollection<SelectableSpecial>();
        LibraryCollectionView.ItemsSource = _selectableItems;

        // Load library items when page appears
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await LoadLibraryItems();
    }

    private async Task LoadLibraryItems()
    {
        try
        {
            var librarySpecials = await _libraryService.LoadLibraryAsync();

            _selectableItems.Clear();
            foreach (var special in librarySpecials)
            {
                _selectableItems.Add(new SelectableSpecial(special));
            }

            // Update info label
            InfoLabel.Text = _selectableItems.Count == 0 
                ? "Library is empty. Add specials by updating the display." 
                : $"Select one or more items ({_selectableItems.Count} available)";

            AddButton.IsEnabled = _selectableItems.Count > 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load library: {ex.Message}", "OK");
        }
    }

    private async void OnAddSelectedClicked(object? sender, EventArgs e)
    {
        var selectedSpecials = _selectableItems
            .Where(s => s.IsSelected)
            .Select(s => new Special 
            { 
                Text = s.Special.Text, 
                Color = s.Special.Color 
            })
            .ToList();

        if (selectedSpecials.Count == 0)
        {
            await DisplayAlert("Info", "Please select at least one item", "OK");
            return;
        }

        // Notify caller with selected items
        OnSpecialsSelected?.Invoke(selectedSpecials);

        // Navigate back
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
