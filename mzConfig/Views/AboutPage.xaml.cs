namespace mzConfigure.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage(ViewModels.AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
