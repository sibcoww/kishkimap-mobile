using NomadGisMobile.Models;
using NomadGisMobile.Services;
using Microsoft.Maui.Storage;

namespace NomadGisMobile;

public partial class ProfilePage : ContentPage
{
    private bool _isLoadingProfile = false;

    public ProfilePage()
    {
        InitializeComponent();
        LoadProfile();
    }

    private async void LoadProfile()
    {
        if (_isLoadingProfile)
            return;

        _isLoadingProfile = true;

        try
        {
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
                // Don't clear token immediately. Ask user what to do.
                var choice = await DisplayAlert("Ошибка", "Не удалось загрузить профиль. Попробовать ещё или выйти?", "Повторить", "Выйти");
                if (choice)
                {
                    // Retry
                    _isLoadingProfile = false;
                    LoadProfile();
                    return;
                }
                else
                {
                    try { SecureStorage.Remove("access_token"); } catch { }
                    await Shell.Current.GoToAsync("login");
                    return;
                }
            }

            // Заполняем текст
            UsernameLabel.Text = me.Username;
            IdLabel.Text = $"ID: {me.Id}";
            EmailLabel.Text = $"Email: {me.Email}";
            LevelLabel.Text = $"Level: {me.Level}";
            XpLabel.Text = $"XP: {me.Experience}";

            // Устанавливаем картинку, если есть URL
            if (!string.IsNullOrWhiteSpace(me.AvatarUrl))
            {
                AvatarImage.Source = ImageSource.FromUri(new Uri(me.AvatarUrl));
            }
        }
        catch (Exception ex)
        {
            // Offer retry/logout rather than immediate navigation
            var retry = await DisplayAlert("Ошибка", "Не удалось загрузить профиль: " + ex.Message, "Повторить", "Выйти");
            if (retry)
            {
                _isLoadingProfile = false;
                LoadProfile();
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
            _isLoadingProfile = false;
        }
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MapPage));
    }

}
