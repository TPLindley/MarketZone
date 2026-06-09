using mzConfigure.ViewModels;

namespace mzConfigure;

public partial class ColorPickerPage : ContentPage
{
    public ColorPickerPage(string currentColor)
    {
        InitializeComponent();
        var viewModel = new ColorPickerViewModel();
        viewModel.HexColor = currentColor;
        BindingContext = viewModel;
    }
}
