using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using NomadGisMobile.Services;
using System;
using System.Threading.Tasks;

namespace NomadGisMobile;

public partial class RegisterPage : ContentPage
{
    private bool _isBusy = false;

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;

        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;

        var email = EmailEntry.Text?.Trim() ?? "";
        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";
        var confirm = ConfirmPasswordEntry.Text ?? "";

        // --- Валидация ---
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Введите email.");
            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Введите имя пользователя.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Пароль должен быть ? 6 символов.");
            return;
        }

        if (password != confirm)
        {
            ShowError("Пароли не совпадают.");
            return;
        }

        _isBusy = true;
        SetLoading(true);

        try
        {
            // deviceId обязателен API
            string deviceId = await GetOrCreateDeviceIdAsync();

            var api = new ApiClient();

            // --- 1. Отправляем запрос регистрации ---
            var body = new
            {
                email = email,
                password = password,
                username = username,
                deviceId = deviceId
            };

            var reg = await api.PostAsync<object>("/api/v1/auth/register", body);

            if (reg == null)
            {
                ShowError("Регистрация не удалась. Попробуйте позже.");
                return;
            }

            // --- 2. Авто-логин через существующий AuthService ---
            var authService = new AuthService(api);

            var login = await authService.LoginAsync(email, password);

            if (login == null || string.IsNullOrWhiteSpace(login.AccessToken))
            {
                await DisplayAlert("Регистрация", "Аккаунт создан! Теперь войдите вручную.", "OK");
                await Shell.Current.GoToAsync("login");
                return;
            }

            // --- 3. Сохранение токена ---
            await SecureStorage.SetAsync("access_token", login.AccessToken);

            if (!string.IsNullOrWhiteSpace(login.RefreshToken))
                await SecureStorage.SetAsync("refresh_token", login.RefreshToken);

            // --- 4. Переход в приложение ---
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка регистрации: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            SetLoading(false);
        }
    }

    private async Task<string> GetOrCreateDeviceIdAsync()
    {
        var id = await SecureStorage.GetAsync("device_id");
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync("device_id", id);
        }
        return id;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool v)
    {
        RegisterButton.IsEnabled = !v;
        LoadingIndicator.IsRunning = v;
        LoadingIndicator.IsVisible = v;
    }

    private async void OnLoginNavTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("login");
    }
}
