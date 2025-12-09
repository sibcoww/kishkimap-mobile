using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NomadGisMobile.Models;
using NomadGisMobile.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NomadGisMobile;

public partial class AchievementsPage : ContentPage
{
    private bool _isLoading = false;

    public AchievementsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            await DisplayAlert(
                "Требуется вход",
                "Достижения доступны только для авторизованных пользователей.",
                "Войти");

            await Shell.Current.GoToAsync("login");
            return;
        }

        // если пользователь залогинен – загружаем ачивки как раньше
        await LoadAchievementsAsync();
    }

    private async Task LoadAchievementsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            StatusLabel.Text = "Загружаем достижения...";

            // базовый клиент без токена
            var api = new ApiClient();
            var achievementsService = new AchievementsService(api);

            // все ачивки
            var all = await achievementsService.GetAllAsync()
                      ?? new List<AchievementResponse>();

            // мои ачивки (если есть токен)
            var myAchievements = new List<AchievementResponse>();
            var token = await SecureStorage.GetAsync("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                api.SetBearerToken(token);
                try
                {
                    var tmp = await achievementsService.GetMyAsync();
                    if (tmp != null)
                        myAchievements = tmp;
                }
                catch
                {
                    // если не удалось получить личные ачивки - не падаем
                }
            }

            AchievementsList.Children.Clear();

            if (all.Count == 0)
            {
                StatusLabel.Text = "Достижений пока нет.";
                return;
            }

            if (string.IsNullOrEmpty(token))
            {
                StatusLabel.Text = "Вы не авторизованы. Показан общий список достижений.";
            }
            else
            {
                StatusLabel.Text = $"Открыто: {myAchievements.Count} из {all.Count}";
            }

            // множество ID открытых ачивок
            var unlockedIds = new HashSet<string>(
                myAchievements
                    .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                    .Select(a => a.Id!),
                StringComparer.OrdinalIgnoreCase);

            foreach (var a in all)
            {
                bool isUnlocked = a.Id != null && unlockedIds.Contains(a.Id);

                var frame = new Frame
                {
                    CornerRadius = 18,
                    Padding = 16,
                    BackgroundColor = isUnlocked ? Colors.White : Color.FromArgb("#E5E7EB"),
                    HasShadow = true,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var vertical = new VerticalStackLayout
                {
                    Spacing = 4
                };

                // заголовок
                vertical.Children.Add(new Label
                {
                    Text = string.IsNullOrWhiteSpace(a.Title) ? "Без названия" : a.Title,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 18,
                    TextColor = Colors.Black
                });

                // описание
                if (!string.IsNullOrWhiteSpace(a.Description))
                {
                    vertical.Children.Add(new Label
                    {
                        Text = a.Description,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#4B5563")
                    });
                }

                // нижняя строка: очки и статус
                var bottomRow = new HorizontalStackLayout
                {
                    Spacing = 8,
                    VerticalOptions = LayoutOptions.Center
                };

                bottomRow.Children.Add(new Label
                {
                    Text = $"+{a.RewardPoints} XP",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#3B82F6")
                });

                bottomRow.Children.Add(new Label
                {
                    Text = isUnlocked ? "Получено" : "Не получено",
                    FontSize = 13,
                    TextColor = isUnlocked ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF")
                });

                vertical.Children.Add(bottomRow);
                frame.Content = vertical;

                // Жест нажатия
                var tap = new TapGestureRecognizer
                {
                    Command = new Command(async () => await ShowAchievementDetailsAsync(a, isUnlocked))
                };
                frame.GestureRecognizers.Add(tap);

                if (!isUnlocked)
                {
                    frame.Opacity = 0.8;
                }

                AchievementsList.Children.Add(frame);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", "Не удалось загрузить достижения: " + ex.Message, "OK");
            StatusLabel.Text = "Не удалось загрузить достижения.";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
        }

    }
    private async Task ShowAchievementDetailsAsync(AchievementResponse a, bool isUnlocked)
    {
        var title = string.IsNullOrWhiteSpace(a.Title) ? "Достижение" : a.Title;

        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(a.Description))
        {
            sb.AppendLine(a.Description);
            sb.AppendLine();
        }

        sb.AppendLine(isUnlocked ? "Статус: получено ?" : "Статус: не получено ?");
        sb.AppendLine($"Награда: +{a.RewardPoints} XP");

        if (!string.IsNullOrWhiteSpace(a.Code))
        {
            sb.AppendLine();
            sb.AppendLine($"Код ачивки: {a.Code}");
        }

        await DisplayAlert(title, sb.ToString(), "OK");
    }

}
