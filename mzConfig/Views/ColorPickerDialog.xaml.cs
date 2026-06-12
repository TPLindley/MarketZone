using Microsoft.Maui.Controls;

namespace mzConfigure.Views;

public partial class ColorPickerDialog : ContentPage
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

    public string? SelectedColorHex { get; private set; }
    public string? SelectedColorName { get; private set; }

    public ColorPickerDialog()
    {
        InitializeComponent();
        CreateColorCircles();
    }

    private void CreateColorCircles()
    {
        int index = 0;
        foreach (var color in _colors)
        {
            int row = index / 4;  // 4 colors per row
            int col = index % 4;  // Column within the row

            var frame = new Frame
            {
                WidthRequest = 80,
                HeightRequest = 80,
                CornerRadius = 40,
                Padding = 0,
                Margin = 10,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb(color.Value),
                HasShadow = true,
                BorderColor = Microsoft.Maui.Graphics.Colors.White,
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

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnColorSelected(color.Key, color.Value);
            frame.GestureRecognizers.Add(tapGesture);

            Grid.SetRow(frame, row);
            Grid.SetColumn(frame, col);
            ColorGrid.Children.Add(frame);

            index++;
        }
    }

    private async void OnColorSelected(string colorName, string colorHex)
    {
        SelectedColorName = colorName;
        SelectedColorHex = colorHex;
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        SelectedColorHex = null;
        SelectedColorName = null;
        await Navigation.PopModalAsync();
    }
}
