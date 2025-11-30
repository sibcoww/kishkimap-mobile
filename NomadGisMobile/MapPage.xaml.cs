using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NomadGisMobile.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsuiMap = Mapsui.Map;

namespace NomadGisMobile;

public partial class MapPage : ContentPage
{
    private MapControl _mapControl;

    public MapPage()
    {
        InitializeComponent();

        // MapControl теперь берём из XAML
        _mapControl = MapView;

        // обработчик тапа по карте/маркеру
        _mapControl.Info += OnMapInfo;

        InitMap();
    }

    // ----------------- ИНИЦИАЛИЗАЦИЯ КАРТЫ -----------------

    private void InitMap()
    {
        // убираем лог-плашку
        LoggingWidget.ShowLoggingInMap = ActiveMode.No;

        _mapControl.Map = new MapsuiMap
        {
            CRS = "EPSG:3857"
        };

        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map.Layers.Add(tileLayer);

        // чистим виджеты с FPS/Performance, если есть
        if (_mapControl.Map.Widgets is ConcurrentQueue<IWidget> queue)
        {
            var all = queue.ToList();
            while (queue.TryDequeue(out _)) { }

            foreach (var w in all)
            {
                var name = w.GetType().Name;
                if (name.Contains("Performance", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Fps", StringComparison.OrdinalIgnoreCase))
                    continue;

                queue.Enqueue(w);
            }
        }

        _ = LoadPointsAsync();
    }

    // ----------------- ЗАГРУЗКА ТОЧЕК -----------------

    private async Task LoadPointsAsync()
    {
        try
        {
            if (_mapControl?.Map == null)
                return;

            var map = _mapControl.Map;

            var token = await SecureStorage.GetAsync("access_token");

            var api = new ApiClient();
            if (!string.IsNullOrWhiteSpace(token))
                api.SetBearerToken(token);

            var service = new PointsService(api);
            var points = await service.GetPointsAsync();

            if (points == null || points.Count == 0)
                return;

            // наш PNG-маркер
            var img = new Mapsui.Styles.Image
            {
                // файл: Resources/Images/pointer2.png, Build Action: MauiImage
                Source = "embedded://NomadGisMobile.Resources.Images.pointer2.png"
            };

            var imageStyle = new ImageStyle
            {
                Image = img,
                SymbolScale = 0.04f   // размер маркера
            };

            var features = new List<IFeature>();

            foreach (var p in points)
            {
                if (double.IsNaN(p.Latitude) || double.IsNaN(p.Longitude))
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                var point = new MPoint(x, y);

                var feature = new PointFeature(point);

                // сохраняем ВСЕ нужные данные в feature
                feature["id"] = p.Id ?? "";
                feature["name"] = p.Name ?? "";
                feature["description"] = p.Description ?? "";
                feature["lat"] = p.Latitude;
                feature["lon"] = p.Longitude;

                feature.Styles.Clear();
                feature.Styles.Add(imageStyle);

                features.Add(feature);
            }


            if (features.Count == 0)
                return;

            var provider = new MemoryProvider(features);
            var layer = new Layer("Points")
            {
                DataSource = provider,
                Style = null // без стандартного белого кружка
            };

            map.Layers.Add(layer);

            // лёгкий авто-зум по всем точкам (если нужно)
            var extent = provider.GetExtent();
            if (extent != null)
            {
                // чуть-чуть отъедем, чтобы все влезли
                var center = extent.Centroid;
                var maxRes = map.Navigator.Resolutions?.FirstOrDefault() ?? map.Navigator.Viewport.Resolution;
                map.Navigator.CenterOnAndZoomTo(center, maxRes / 50, 500);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка загрузки точек", ex.ToString(), "OK");
        }
    }

    // ----------------- ТАП ПО МАРКЕРУ -----------------

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        if (_mapControl?.Map == null)
            return;

        var pointsLayer = _mapControl.Map.Layers
            .FirstOrDefault(l => l.Name == "Points");

        if (pointsLayer == null)
            return;

        var mapInfo = e.GetMapInfo(new[] { pointsLayer });
        if (mapInfo?.Feature == null)
            return;

        var f = mapInfo.Feature;

        var name = f["name"]?.ToString();
        var description = f["description"]?.ToString();

        // плавно центрируем карту на маркере
        var worldPos = mapInfo.WorldPosition;
        if (worldPos != null)
        {
            _mapControl.Map.Navigator.CenterOn(worldPos, duration: 400);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PointTitleLabel.Text =
                string.IsNullOrWhiteSpace(name) ? "Место" : name;

            PointDescriptionLabel.Text =
                string.IsNullOrWhiteSpace(description)
                    ? "Описание отсутствует"
                    : description;

            PointCard.IsVisible = true;
        });
    }

}
