using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NomadGisMobile.Models;
using NomadGisMobile.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace NomadGisMobile;

public partial class MapPage : ContentPage
{
    private MapControl _mapControl = null!;

    public MapPage()
    {
        InitializeComponent();

        try
        {
            // создаём MapControl и базовую карту
            _mapControl = new MapControl
            {
                Map = new Mapsui.Map()
            };

            _mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // показываем карту на странице
            Content = _mapControl;
        }
        catch (Exception ex)
        {
            // Если что-то идёт не так при инициализации карты — покажем ошибку и не дадим падать дальше
            Content = new Label
            {
                Text = "Ошибка инициализации карты: " + ex.Message,
                TextColor = Colors.Red,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await LoadPointsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка карты", ex.ToString(), "OK");
        }
    }

    private async Task LoadPointsAsync()
    {
        try
        {
            // если вдруг по какой-то причине Map ещё null — просто выходим
            if (_mapControl == null || _mapControl.Map == null)
                return;

            var map = _mapControl.Map;

            // токен (может быть null — это не ошибка)
            var token = await SecureStorage.GetAsync("access_token");

            var api = new ApiClient();
            if (!string.IsNullOrWhiteSpace(token))
                api.SetBearerToken(token);

            var service = new PointsService(api);
            var points = await service.GetPointsAsync();

            if (points == null || points.Count == 0)
                return;

            var features = new List<IFeature>();

            foreach (var p in points)
            {
                if (double.IsNaN(p.Latitude) || double.IsNaN(p.Longitude))
                    continue;

                // lat/lon -> проекция
                var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                var point = new MPoint(x, y);

                var feature = new PointFeature(point);
                feature["name"] = p.Name ?? string.Empty;
                feature["id"] = p.Id ?? string.Empty;

                features.Add(feature);
            }

            if (features.Count == 0)
                return;

            var provider = new MemoryProvider(features);

            var layer = new Layer("Points")
            {
                DataSource = provider,
                Style = new SymbolStyle
                {
                    SymbolScale = 1.0,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 1)
                }
            };

            map.Layers.Add(layer);

            // НИКАКОГО центрирования тут нет — только добавляем слой
        }
        catch (Exception ex)
        {
            // Ловим любые ошибки загрузки точек и показываем
            await DisplayAlert("Ошибка загрузки точек", ex.ToString(), "OK");
        }
    }
}
