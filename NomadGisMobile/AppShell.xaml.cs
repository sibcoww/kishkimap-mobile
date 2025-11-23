namespace NomadGisMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));

            // Register login route (MainPage) so Shell navigation to 'login' works
            Routing.RegisterRoute("login", typeof(MainPage));
        }
    }
}
