using mzWheeler.ViewModels;

namespace mzWheeler.Views;

public partial class DashboardPageSimple : ContentPage
{
    public DashboardPageSimple()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel();
    }
}
