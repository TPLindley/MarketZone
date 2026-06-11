using System.ComponentModel;
using System.Runtime.CompilerServices;
using mzConfigure.Models;

namespace mzConfigure.Models;

/// <summary>
/// Wrapper for Special that adds selection state for multi-select UI
/// </summary>
public class SelectableSpecial : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableSpecial(Special special)
    {
        Special = special;
    }

    public Special Special { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    // Expose Special properties for binding
    public string Text => Special.Text;
    public string Color => Special.Color;
    public Microsoft.Maui.Graphics.Color ColorValue => Special.ColorValue;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
