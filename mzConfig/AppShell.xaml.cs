namespace mzConfigure
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute("libraryselection", typeof(Views.LibrarySelectionPage));
        }
    }
}
