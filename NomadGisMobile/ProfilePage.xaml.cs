using NomadGisMobile.Models;
using NomadGisMobile.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace NomadGisMobile;

public partial class ProfilePage : ContentPage
{
    private bool _isLoadingProfile = false;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadProfile();
    }

    private async Task LoadProfile()
    {
        if (_isLoadingProfile)
            return;

        _isLoadingProfile = true;

        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var token = await SecureStorage.GetAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                var toLogin = await DisplayAlert("Ошибка", "Токен не найден, авторизуйтесь заново", "Перейти", "Отмена");
                if (toLogin)
                    await Shell.Current.GoToAsync("login");
                return;
            }

            var api = new ApiClient();
            api.SetBearerToken(token);

            var service = new ProfileService(api);
            var me = await service.GetMeAsync();

            if (me == null)
            {
                var choice = await DisplayAlert("Ошибка", "Не удалось загрузить профиль. Попробовать ещё или выйти?", "Повторить", "Выйти");
                if (choice)
                {
                    _isLoadingProfile = false;
                    await LoadProfile();
                    return;
                }
                else
                {
                    try { SecureStorage.Remove("access_token"); } catch { }
                    await Shell.Current.GoToAsync("login");
                    return;
                }
            }

            // Small delay to let UI finish layout
            await Task.Delay(50);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                UsernameLabel.Text = me.Username;
                IdLabel.Text = $"ID: {me.Id}";
                EmailLabel.Text = $"Email: {me.Email}";
                LevelLabel.Text = $"Level: {me.Level}";
                XpLabel.Text = $"XP: {me.Experience}";
            });

            // load avatar safely
            if (!string.IsNullOrWhiteSpace(me.AvatarUrl))
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AvatarImage.Source = ImageSource.FromUri(new Uri(me.AvatarUrl));
                    });
                }
                catch
                {
                    // ignore image load errors
                }
            }
        }
        catch (Exception ex)
        {
            var retry = await DisplayAlert("Ошибка", "Не удалось загрузить профиль: " + ex.Message, "Повторить", "Выйти");
            if (retry)
            {
                _isLoadingProfile = false;
                await LoadProfile();
                return;
            }
            else
            {
                try { SecureStorage.Remove("access_token"); } catch { }
                await Shell.Current.GoToAsync("login");
                return;
            }
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoadingProfile = false;
        }
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MapPage));
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Редактировать", "Здесь можно реализовать экран редактирования профиля.", "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход", "Выйти из аккаунта?", "Да", "Нет");
        if (!confirm)
            return;

        try
        {
            SecureStorage.Remove("access_token");
        }
        catch { }

        await Shell.Current.GoToAsync("login");
    }

}
