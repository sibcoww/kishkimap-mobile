using NomadGisMobile.Services;
using NomadGisMobile.Models;
using Microsoft.Maui.Storage;

namespace NomadGisMobile;

public partial class MainPage : ContentPage
{
    private readonly AuthService _authService;

    public MainPage()
    {
        InitializeComponent();
        Shell.SetTabBarIsVisible(this, false); // скрыть вкладки на логине

        // Инициализация, чтобы избежать NullReferenceException при авторизации
        _authService = new AuthService(new ApiClient());
    }


    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            if (StatusLabel != null)
                StatusLabel.Text = "";

            if (EmailEntry == null || PasswordEntry == null)
            {
                await DisplayAlert("Ошибка", "UI элементы не инициализированы", "OK");
                return;
            }

            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                if (StatusLabel != null)
                    StatusLabel.Text = "Введите email и пароль";
                return;
            }

            if (_authService == null)
            {
                await DisplayAlert("Ошибка", "_authService == null", "OK");
                return;
            }

            var result = await _authService.LoginAsync(email, password);

            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                if (StatusLabel != null)
                    StatusLabel.Text = "Ошибка авторизации";
            }
            else
            {
                await SecureStorage.SetAsync("access_token", result.AccessToken);

                // переходим на вкладку "Карта" и показываем нижнее меню
                await Shell.Current.GoToAsync("//map");
            }
        }
        catch (Exception ex)
        {
            // Показать стек в UI, чтобы понять где именно NRE происходит
            await DisplayAlert("Unhandled exception", ex.ToString(), "OK");
        }
    }
}
