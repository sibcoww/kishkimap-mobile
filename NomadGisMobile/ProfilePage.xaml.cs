using NomadGisMobile.Models;
using NomadGisMobile.Services;

namespace NomadGisMobile;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        LoadProfile();
    }

    private async void LoadProfile()
    {
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert("Ошибка", "Токен не найден, авторизуйтесь заново", "OK");
            await Shell.Current.GoToAsync(".."); // назад
            return;
        }

        var api = new ApiClient();
        api.SetBearerToken(token);

        var service = new ProfileService(api);
        var me = await service.GetMeAsync();

        if (me == null)
        {
            await DisplayAlert("Ошибка", "Не удалось загрузить профиль", "OK");
            return;
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
    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MapPage));
    }

}
