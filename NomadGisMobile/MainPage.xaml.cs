using Microsoft.Maui.Controls;
using NomadGisMobile.Services;
using Microsoft.Maui.Storage;
using System;

namespace NomadGisMobile;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoginButton.IsEnabled = false;

        try
        {
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Ошибка", "Введите почту и пароль", "OK");
                return;
            }

            var api = new ApiClient();
            var auth = new AuthService(api);

            var result = await auth.LoginAsync(email, password);

            if (result == null)
            {
                await DisplayAlert("Ошибка", "Неверные данные", "OK");
                return;
            }

            await SecureStorage.SetAsync("access_token", result.AccessToken);

            await Shell.Current.GoToAsync("//profile");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoginButton.IsEnabled = true;
        }
    }
}
