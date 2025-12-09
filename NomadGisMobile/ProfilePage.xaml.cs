using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NomadGisMobile.Models;
using NomadGisMobile.Services;
using System;
using System.Threading.Tasks;

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
        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        if (_isLoadingProfile)
            return;

        _isLoadingProfile = true;

        try
        {
            var token = await SecureStorage.GetAsync("access_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                // ---- ГОСТЕВОЙ РЕЖИМ ----
                UsernameLabel.Text = "Гость";
                EmailLabel.Text = "Вы не авторизованы";
                LevelLabel.Text = "-";
                XpTotalLabel.Text = "-";
                AvatarImage.Source = "profile_placeholder.png"; // или любой дефолтный

                AuthButtonsPanel.IsVisible = false;
                GuestButtonsPanel.IsVisible = true;

                _isLoadingProfile = false;
                return;
            }

            // ---- ЗАЛОГИНЕННЫЙ ПОЛЬЗОВАТЕЛЬ ----
            var api = new ApiClient();
            api.SetBearerToken(token);

            var profileService = new ProfileService(api);
            ProfileMeDto? me = await profileService.GetMeAsync();

            if (me == null)
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить профиль.", "OK");
                // считаем его гостем
                AuthButtonsPanel.IsVisible = false;
                GuestButtonsPanel.IsVisible = true;
                UsernameLabel.Text = "Гость";
                EmailLabel.Text = "Вы не авторизованы";
                LevelLabel.Text = "-";
                XpTotalLabel.Text = "-";
                return;
            }

            AuthButtonsPanel.IsVisible = true;
            GuestButtonsPanel.IsVisible = false;

            UsernameLabel.Text = string.IsNullOrWhiteSpace(me.Username) ? "Без имени" : me.Username;
            EmailLabel.Text = string.IsNullOrWhiteSpace(me.Email) ? "" : $"Почта: {me.Email}";
            LevelLabel.Text = me.Level.ToString();
            XpTotalLabel.Text = me.Experience.ToString();

            if (!string.IsNullOrWhiteSpace(me.AvatarUrl))
            {
                try
                {
                    AvatarImage.Source = ImageSource.FromUri(new Uri(me.AvatarUrl));
                }
                catch
                {
                    // игнорируем ошибку загрузки аватара
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
            AuthButtonsPanel.IsVisible = false;
            GuestButtonsPanel.IsVisible = true;
        }
        finally
        {
            _isLoadingProfile = false;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход", "Выйти из аккаунта?", "Да", "Нет");
        if (!confirm)
            return;

        try
        {
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
        }
        catch { }

        // после выхода – гость
        AuthButtonsPanel.IsVisible = false;
        GuestButtonsPanel.IsVisible = true;
        UsernameLabel.Text = "Гость";
        EmailLabel.Text = "Вы не авторизованы";
        LevelLabel.Text = "-";
        XpTotalLabel.Text = "-";

        await Shell.Current.GoToAsync("login");
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Редактирование", "Редактирование профиля будет добавлено позже.", "OK");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("login");
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}
