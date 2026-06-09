namespace mzConfigure
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("colorpicker", typeof(ColorPickerPage));
        }
    }
}
