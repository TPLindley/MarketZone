using mzWheeler.ViewModels;

namespace mzWheeler.Views;

public partial class ConditionsPageSimple : ContentPage
{
    public ConditionsPageSimple()
    {
        InitializeComponent();
        BindingContext = new ConditionsViewModel();
    }
}
