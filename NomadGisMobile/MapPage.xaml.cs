using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Utilities;
using NomadGisMobile.Models;
using NomadGisMobile.Services;

namespace NomadGisMobile;

public partial class MapPage : ContentPage
{
    private MapControl _map;

    public MapPage()
    {
        InitializeComponent();
        InitMap();
    }

    private void InitMap()
    {
        // создаём MapControl
        _map = new MapControl
        {
            Map = new Mapsui.Map()
        };

        // базовый слой OSM
        _map.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // показываем карту
        Content = _map;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPointsAsync();
    }

    private async Task LoadPointsAsync()
    {
        if (_map == null || _map.Map == null)
            return;

        var map = _map.Map;

        // получаем токен
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

            var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
            var mp = new MPoint(x, y);

            var feature = new PointFeature(mp);
            feature["name"] = p.Name ?? "";
            feature["id"] = p.Id ?? "";

            features.Add(feature);
        }

        if (features.Count == 0)
            return;

        var provider = new MemoryProvider(features);

        var layer = new Layer("Points")
        {
            DataSource = provider,
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 1)
            }
        };

        map.Layers.Add(layer);

        // тут НИЧЕГО не центрируем — просто оставляем карту как есть
        // позже можно будет добавить центрирование под твою версию Mapsui
    }
}
