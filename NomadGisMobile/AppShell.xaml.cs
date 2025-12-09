namespace NomadGisMobile
{
    public partial class AppShell : Shell
    {
        public static RoutesPage? RoutesPageInstance { get; private set; }
        public AppShell()
        {
            InitializeComponent();

            // Маршрут страницы логина (MainPage)
            Microsoft.Maui.Controls.Routing.RegisterRoute("login", typeof(MainPage));

            // Дополнительные маршруты, если где-то используешь GoToAsync(nameof(...))
            Microsoft.Maui.Controls.Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Microsoft.Maui.Controls.Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
            Microsoft.Maui.Controls.Routing.RegisterRoute(nameof(RoutesPage), typeof(RoutesPage));
            Microsoft.Maui.Controls.Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));

        }
    }
}
