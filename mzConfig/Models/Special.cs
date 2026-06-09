using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace mzConfigure.Models;

public class Special : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private string _color = "#FFFFFF";

    [JsonPropertyName("text")]
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("color")]
    public string Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorValue));
        }
    }

    [JsonIgnore]
    public Microsoft.Maui.Graphics.Color ColorValue
    {
        get
        {
            try
            {
                return Microsoft.Maui.Graphics.Color.FromArgb(_color);
            }
            catch
            {
                return Microsoft.Maui.Graphics.Colors.White;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
