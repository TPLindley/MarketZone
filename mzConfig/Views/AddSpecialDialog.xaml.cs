using Microsoft.Maui.Controls;
using mzConfigure.Models;
using mzConfigure.Services;

namespace mzConfigure.Views;

public partial class AddSpecialDialog : ContentPage
{
    private readonly Dictionary<string, string> _colors = new()
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

    private readonly SpecialsLibraryService _libraryService;
    private List<Special> _librarySpecials = new();
    private List<Special> _existingSpecials = new();
    private string _selectedColorHex = "#FFFFFF";
    private string _selectedColorName = "White";
    private Frame? _selectedColorFrame;

    public string? SpecialText { get; private set; }
    public string? SpecialColorHex { get; private set; }
    public string? SpecialColorName { get; private set; }

    public AddSpecialDialog(List<Special>? existingSpecials = null)
    {
        InitializeComponent();
        _libraryService = new SpecialsLibraryService();
        _existingSpecials = existingSpecials ?? new();

        // Set initial text color to white (default selection)
        TextEntry.TextColor = Microsoft.Maui.Graphics.Colors.White;

        CreateColorCircles();
        _ = LoadLibrary();
    }

    private async Task LoadLibrary()
    {
        try
        {
            _librarySpecials = await _libraryService.LoadLibraryAsync();
            Log.Info($"AddSpecialDialog: Loaded {_librarySpecials.Count} specials from library");

            if (_librarySpecials.Count > 0)
            {
                // Filter out specials that are already in the existing list
                var availableSpecials = _librarySpecials
                    .Where(lib => !_existingSpecials.Any(existing => 
                        existing.Text.Trim().Equals(lib.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                Log.Info($"AddSpecialDialog: {availableSpecials.Count} specials available after filtering");

                // Add filtered library items to picker
                foreach (var special in availableSpecials)
                {
                    LibraryPicker.Items.Add(special.Text);
                }

                // Update the library specials list to only include available ones
                _librarySpecials = availableSpecials;
            }
            else
            {
                Log.Info("AddSpecialDialog: Library is empty, no specials to display");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "AddSpecialDialog: Failed to load library");
        }
    }

    private void CreateColorCircles()
    {
        int index = 0;
        foreach (var color in _colors)
        {
            int row = index / 4;  // 4 colors per row
            int col = index % 4;  // Column within the row

            var isWhite = color.Key == "White";

            var frame = new Frame
            {
                WidthRequest = 70,
                HeightRequest = 70,
                CornerRadius = 35,
                Padding = 0,
                Margin = 5,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb(color.Value),
                HasShadow = true,
                BorderColor = isWhite && color.Key == _selectedColorName 
                    ? Microsoft.Maui.Graphics.Colors.Pink 
                    : Microsoft.Maui.Graphics.Colors.White,
                Content = new Label
                {
                    Text = color.Key,
                    FontSize = 10,
                    TextColor = color.Key == "White" || color.Key == "Yellow" || color.Key == "Lime" || color.Key == "Cyan"
                        ? Microsoft.Maui.Graphics.Colors.Black
                        : Microsoft.Maui.Graphics.Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            };

            // Store reference to White frame for default selection
            if (color.Key == "White")
            {
                _selectedColorFrame = frame;
                frame.BorderColor = Microsoft.Maui.Graphics.Colors.Pink;
            }

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnColorSelected(color.Key, color.Value, frame);
            frame.GestureRecognizers.Add(tapGesture);

            Grid.SetRow(frame, row);
            Grid.SetColumn(frame, col);
            ColorGrid.Children.Add(frame);

            index++;
        }
    }

    private void OnColorSelected(string colorName, string colorHex, Frame selectedFrame)
    {
        // Reset previous selection border
        if (_selectedColorFrame != null)
        {
            _selectedColorFrame.BorderColor = Microsoft.Maui.Graphics.Colors.White;
        }

        // Highlight new selection
        selectedFrame.BorderColor = Microsoft.Maui.Graphics.Colors.Pink;
        _selectedColorFrame = selectedFrame;

        _selectedColorName = colorName;
        _selectedColorHex = colorHex;

        // Update the text entry color to match the selected color
        TextEntry.TextColor = Microsoft.Maui.Graphics.Color.FromArgb(colorHex);
    }

    private void OnLibraryItemSelected(object sender, EventArgs e)
    {
        if (LibraryPicker.SelectedIndex >= 0 && LibraryPicker.SelectedIndex < _librarySpecials.Count)
        {
            var selected = _librarySpecials[LibraryPicker.SelectedIndex];
            TextEntry.Text = selected.Text;

            // Try to match the color and select it
            var matchingColor = _colors.FirstOrDefault(c => 
                c.Value.Equals(selected.Color, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchingColor.Key))
            {
                // Find the frame for this color and select it
                foreach (var child in ColorGrid.Children)
                {
                    if (child is Frame frame && frame.Content is Label label && label.Text == matchingColor.Key)
                    {
                        OnColorSelected(matchingColor.Key, matchingColor.Value, frame);
                        break;
                    }
                }
            }
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var text = TextEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert("Error", "Please enter text for the special.", "OK");
            return;
        }

        SpecialText = text;
        SpecialColorHex = _selectedColorHex;
        SpecialColorName = _selectedColorName;

        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        SpecialText = null;
        SpecialColorHex = null;
        SpecialColorName = null;

        await Navigation.PopModalAsync();
    }
}
