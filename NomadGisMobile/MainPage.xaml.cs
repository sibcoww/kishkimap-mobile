using NomadGisMobile.Services;
using NomadGisMobile.Models;

namespace NomadGisMobile;

public partial class MainPage : ContentPage
{
    private readonly AuthService _authService;

    public MainPage()
    {
        InitializeComponent();
        Shell.SetTabBarIsVisible(this, false); // скрыть вкладки на логине
    }


    private async void OnLoginClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "";

        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusLabel.Text = "Введите email и пароль";
            return;
        }

        var result = await _authService.LoginAsync(email, password);

        if (result == null || string.IsNullOrEmpty(result.AccessToken))
        {
            StatusLabel.Text = "Ошибка авторизации";
        }
        else
        {
            await SecureStorage.SetAsync("access_token", result.AccessToken);

            // переходим на вкладку "Карта" и показываем нижнее меню
            await Shell.Current.GoToAsync("//map");
        }

    }
}
