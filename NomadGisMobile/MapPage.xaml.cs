using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NomadGisMobile.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MapsuiMap = Mapsui.Map;

namespace NomadGisMobile;

public partial class MapPage : ContentPage
{
    private MapControl _mapControl;

    public MapPage()
    {
        InitializeComponent();

        // 1) Создаём MapControl программно
        _mapControl = new MapControl();

        // 2) Подписываемся на нажатия по карте
        _mapControl.Info += OnMapInfo;

        // 3) Ставим карту как содержимое страницы
        Content = _mapControl;

        // 4) Инициализация карты
        InitMap();
    }

    // ----------------- ИНИЦИАЛИЗАЦИЯ КАРТЫ -----------------

    private void InitMap()
    {
        LoggingWidget.ShowLoggingInMap = ActiveMode.No;
        _mapControl.Map = new MapsuiMap
        {
            CRS = "EPSG:3857"
        };

        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map.Layers.Add(tileLayer);

        // 🔻 Убираем виджет FPS/Mean/Min/... из Widgets
        if (_mapControl.Map.Widgets is ConcurrentQueue<IWidget> queue)
        {
            // копируем все виджеты во временный список
            var all = queue.ToList();

            // полностью очищаем очередь
            while (queue.TryDequeue(out _)) { }

            // возвращаем обратно только те, которые нам нужны
            foreach (var w in all)
            {
                var typeName = w.GetType().Name;

                // пропускаем все, в имени которых есть Performance или Fps
                if (typeName.Contains("Performance", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Fps", StringComparison.OrdinalIgnoreCase))
                    continue;

                queue.Enqueue(w);
            }
        }
        // 🔺 конец блока очистки виджетов

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

            // Берём токен
            var token = await SecureStorage.GetAsync("access_token");

            var api = new ApiClient();
            if (!string.IsNullOrWhiteSpace(token))
                api.SetBearerToken(token);

            // Загружаем точки из API
            var service = new PointsService(api);
            var points = await service.GetPointsAsync();

            if (points == null || points.Count == 0)
                return;

            // Загружаем PNG маркер
            var img = new Mapsui.Styles.Image
            {
                Source = "embedded://NomadGisMobile.Resources.Images.pointer2.png"
            };

            var imageStyle = new ImageStyle
            {
                Image = img,
                SymbolScale = 0.02f // уменьшаем маркер до нормального размера
            };

            var features = new List<IFeature>();

            foreach (var p in points)
            {
                if (double.IsNaN(p.Latitude) || double.IsNaN(p.Longitude))
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                var point = new MPoint(x, y);

                var feature = new PointFeature(point);

                // Передадим данные точки в feature
                feature["id"] = p.Id ?? "";
                feature["name"] = p.Name ?? "";
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
                Style = null // отключаем дефолтный белый кружок
            };

            map.Layers.Add(layer);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка загрузки точек", ex.ToString(), "OK");
        }
    }


    // ----------------- ОБРАБОТКА ТАПА ПО МАРКЕРУ -----------------

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        if (_mapControl?.Map == null)
            return;

        // Находим слой точек
        var pointsLayer = _mapControl.Map.Layers
            .FirstOrDefault(l => l.Name == "Points");

        if (pointsLayer == null)
            return;

        // В Mapsui v5: используем GetMapInfo(...), НЕ e.MapInfo
        var mapInfo = e.GetMapInfo(new[] { pointsLayer });
        if (mapInfo?.Feature == null)
            return;

        var f = mapInfo.Feature;

        var name = f["name"]?.ToString() ?? "Место";
        var id = f["id"]?.ToString();
        var lat = f["lat"]?.ToString();
        var lon = f["lon"]?.ToString();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            string text = "";

            if (!string.IsNullOrWhiteSpace(id))
                text += $"ID: {id}\n";
            if (!string.IsNullOrWhiteSpace(lat) && !string.IsNullOrWhiteSpace(lon))
                text += $"Координаты: {lat}, {lon}\n";

            if (string.IsNullOrWhiteSpace(text))
                text = "Нет данных";

            await DisplayAlert(name, text.Trim(), "OK");
        });
    }
}
