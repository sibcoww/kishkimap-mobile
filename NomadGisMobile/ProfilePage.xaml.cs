using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace NomadGisMobile;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();

        // Здесь пока тестовые данные.
        // Потом ты подставишь реальные значения из API.
        int level = 1;
        int xpTotal = 40;      // общий опыт
        int xpForNextLevel = 100; // сколько нужно до следующего уровня

        UsernameLabel.Text = "sibcoww";
        EmailLabel.Text = "sibcoww@example.com";
        LevelLabel.Text = level.ToString();
        XpTotalLabel.Text = xpTotal.ToString();

        // Прогресс до следующего уровня
        double progress = 0;
        if (xpForNextLevel > 0)
            progress = Math.Clamp((double)xpTotal / xpForNextLevel, 0, 1);

        XpProgressBar.Progress = progress;
        XpProgressTextLabel.Text = $"{xpTotal} / {xpForNextLevel} опыта до уровня {level + 1}";

        // Локальный аватар (файл Resources/Images/avatar.png)
        AvatarImage.Source = "avatar.png";
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Редактировать",
            "Экран редактирования профиля будет добавлен позже.",
            "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");
        await Shell.Current.GoToAsync("login");
    }
}
