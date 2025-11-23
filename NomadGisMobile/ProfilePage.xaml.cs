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
                // show Login button so user can navigate to login screen
                LoginButton.IsVisible = true;
                return;
            }

            LoginButton.IsVisible = false;

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
                EmailLabel.Text = $"Почта: {me.Email}";
                LevelLabel.Text = $"Уровень: {me.Level}";
                XpLabel.Text = $"Опыт: {me.Experience}";
                LoginButton.IsVisible = false;
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

    private async void OnLoginNavClicked(object sender, EventArgs e)
    {
        // navigate to login page
        await Shell.Current.GoToAsync("login");
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
