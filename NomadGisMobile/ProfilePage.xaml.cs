using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NomadGisMobile.Models;
using NomadGisMobile.Services;
using System;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Maui.ApplicationModel; // MediaPicker
using System.IO;



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
            // 1. Достаём токен
            var token = await SecureStorage.GetAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                // нет токена – юзер не залогинен
                UsernameLabel.Text = "Гость";
                EmailLabel.Text = "Вы не авторизованы";
                LevelLabel.Text = "-";
                XpTotalLabel.Text = "-";
                _isLoadingProfile = false;
                return;
            }

            // 2. Настраиваем клиента
            var api = new ApiClient();
            api.SetBearerToken(token);

            var profileService = new ProfileService(api);

            // 3. Получаем профиль
            ProfileMeDto? me = await profileService.GetMeAsync();

            if (me == null)
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить профиль.", "OK");
                _isLoadingProfile = false;
                return;
            }

            // 4. Обновляем UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UsernameLabel.Text = string.IsNullOrWhiteSpace(me.Username) ? "Без имени" : me.Username;
                EmailLabel.Text = string.IsNullOrWhiteSpace(me.Email) ? "" : $"Почта: {me.Email}";

                LevelLabel.Text = me.Level.ToString();
                XpTotalLabel.Text = me.Experience.ToString();

                // аватар
                if (!string.IsNullOrWhiteSpace(me.AvatarUrl))
                {
                    try
                    {
                        AvatarImage.Source = ImageSource.FromUri(new Uri(me.AvatarUrl));
                    }
                    catch
                    {
                        // если картинка не загрузилась – просто игнорируем
                    }
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
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
        }
        catch
        {
            // игнор
        }

        // переход на страницу логина
        await Shell.Current.GoToAsync("login");
    }
    private async void OnEditClicked(object sender, EventArgs e)
    {
        // проверяем токен
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert("Авторизация",
                "Вы не авторизованы. Войдите в аккаунт.",
                "OK");
            return;
        }

        // 1. новое имя
        var currentName = UsernameLabel.Text == "Гость" ? "" : UsernameLabel.Text;
        var newName = await DisplayPromptAsync(
            "Имя пользователя",
            "Введите новое имя (оставьте пустым, если не менять):",
            initialValue: currentName);

        if (newName == null)
            return; // пользователь нажал Cancel

        // 2. нужно ли менять пароль?
        string? currentPassword = null;
        string? newPassword = null;

        var changePassword = await DisplayAlert(
            "Смена пароля",
            "Хотите сменить пароль?",
            "Да", "Нет");

        if (changePassword)
        {
            currentPassword = await DisplayPromptAsync(
                "Текущий пароль",
                "Введите текущий пароль:");

            if (currentPassword == null)
                return;

            newPassword = await DisplayPromptAsync(
                "Новый пароль",
                "Введите новый пароль:");

            if (newPassword == null)
                return;
        }

        try
        {
            var api = new ApiClient();
            api.SetBearerToken(token);
            var service = new ProfileService(api);

            await service.UpdateProfileAsync(
                string.IsNullOrWhiteSpace(newName) ? null : newName,
                currentPassword,
                newPassword);

            await DisplayAlert("Профиль", "Данные профиля обновлены.", "OK");

            // перечитываем профиль, чтобы обновилось имя и т.п.
            await LoadProfileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка",
                $"Не удалось обновить профиль: {ex.Message}",
                "OK");
        }
    }

    //private async Task ChangeAvatarAsync()
    //{
    //    try
    //    {
    //        var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
    //        {
    //            Title = "Выберите фото для аватара"
    //        });

    //        if (photo == null)
    //            return;

    //        using var stream = await photo.OpenReadAsync();

    //        var token = await SecureStorage.GetAsync("access_token");
    //        if (string.IsNullOrEmpty(token))
    //        {
    //            await DisplayAlert("Авторизация", "Вы не авторизованы.", "OK");
    //            return;
    //        }

    //        var api = new ApiClient();
    //        api.SetBearerToken(token);
    //        var profileService = new ProfileService(api);

    //        // если сервер недоволен — здесь вылетит Exception с текстом ответа
    //        await profileService.UploadAvatarAsync(stream, photo.FileName);

    //        // если всё ок – перезагружаем профиль
    //        await LoadProfileAsync();
    //        await DisplayAlert("Аватар", "Аватар успешно обновлён.", "OK");
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Ошибка загрузки аватара", ex.Message, "OK");
    //    }
    //}



}
