using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace mzConfigure.ViewModels;

public class ColorPickerViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private Color _selectedColor = Colors.White;
    private string _hexColor = "#FFFFFF";

    public ColorPickerViewModel()
    {
        ConfirmCommand = new Command(async () => await Confirm());
        SelectColorCommand = new Command<string>(SelectColor);

        // Predefined colors for quick selection
        PredefinedColors = new List<ColorOption>
        {
            new ColorOption("White", "#FFFFFF"),
            new ColorOption("Pink", "#FF1595"),
            new ColorOption("Red", "#FF0000"),
            new ColorOption("Orange", "#FF8C00"),
            new ColorOption("Yellow", "#FFFF00"),
            new ColorOption("Lime", "#00FF00"),
            new ColorOption("Cyan", "#00FFFF"),
            new ColorOption("Blue", "#0000FF"),
            new ColorOption("Purple", "#800080"),
            new ColorOption("Magenta", "#FF00FF"),
        };
    }

    public List<ColorOption> PredefinedColors { get; }

    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            HexColor = value.ToHex();
            OnPropertyChanged(nameof(SelectedColor));
        }
    }

    public string HexColor
    {
        get => _hexColor;
        set
        {
            _hexColor = value;
            OnPropertyChanged(nameof(HexColor));
            
            try
            {
                _selectedColor = Color.FromArgb(value);
                OnPropertyChanged(nameof(SelectedColor));
            }
            catch
            {
                // Invalid color format, keep current color
            }
        }
    }

    public ICommand ConfirmCommand { get; }
    public ICommand SelectColorCommand { get; }

    private void SelectColor(string hexColor)
    {
        HexColor = hexColor;
    }

    private async Task Confirm()
    {
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

public class ColorOption
{
    public ColorOption(string name, string hex)
    {
        Name = name;
        Hex = hex;
        Color = Color.FromArgb(hex);
    }

    public string Name { get; set; }
    public string Hex { get; set; }
    public Color Color { get; set; }
}
