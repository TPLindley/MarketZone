using mzConfigure.ViewModels;

namespace mzConfigure
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }

        private async void OnMenuButtonClicked(object sender, EventArgs e)
        {
            var viewModel = BindingContext as MainViewModel;
            if (viewModel == null) return;

            // Build menu options dynamically based on connection state
            var menuItems = new List<string>();

            if (viewModel.IsConnected)
            {
                menuItems.Add("Disconnect");
            }
            else
            {
                menuItems.Add("Connect");
            }

            menuItems.Add("Network Diagnostics");

            if (viewModel.IsConnected)
            {
                menuItems.Add("Clear Specials");
            }

            menuItems.Add($"Orientation: {viewModel.OrientationText}");

            var action = await DisplayActionSheet("Menu", "Cancel", null, menuItems.ToArray());

            if (action == "Connect" || action == "Disconnect")
            {
                viewModel.ConnectCommand.Execute(null);
            }
            else if (action == "Network Diagnostics")
            {
                viewModel.NetworkDiagnosticsCommand.Execute(null);
            }
            else if (action == "Clear Specials")
            {
                viewModel.ClearSpecialsCommand.Execute(null);
            }
            else if (action.StartsWith("Orientation:"))
            {
                // Toggle orientation
                viewModel.IsPortrait = !viewModel.IsPortrait;
            }
        }
    }
}
