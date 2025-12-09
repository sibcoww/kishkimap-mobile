using Mapsui;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Microsoft.Maui.Controls;
using NetTopologySuite.Geometries;
using NomadGisMobile.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Color = Microsoft.Maui.Graphics.Color;
using Colors = Microsoft.Maui.Graphics.Colors;
using MapsuiMap = Mapsui.Map;

namespace NomadGisMobile;

public partial class RoutesPage : ContentPage
{
    private enum TravelMode
    {
        Walking,
        Driving
    }

    private TravelMode _currentMode = TravelMode.Walking;
    private MapControl _miniMapControl;

    // Удобное свойство для доступа к точкам маршрута
    private IList<RoutePoint> RoutePoints => RouteStore.Points;

    public RoutesPage()
    {
        InitializeComponent();

        _miniMapControl = MiniMap;

        InitMiniMap();
        InitHandlers();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            await DisplayAlert(
                "Требуется вход",
                "Маршруты доступны только для авторизованных пользователей.",
                "Войти");

            await Shell.Current.GoToAsync("login");
            return;
        }

        // если токен есть – ведём себя как раньше
        RefreshRoutePointsList();
        UpdateMiniMapMarkers();
    }

    // ---------- ИНИЦИАЛИЗАЦИЯ МИНИ-КАРТЫ ----------

    private void InitMiniMap()
    {
        _miniMapControl.Map = new MapsuiMap
        {
            CRS = "EPSG:3857"
        };

        var tileLayer = OpenStreetMap.CreateTileLayer();
        _miniMapControl.Map.Layers.Add(tileLayer);

        if (_miniMapControl.Map.Widgets is ConcurrentQueue<IWidget> queue)
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
    }

    private void InitHandlers()
    {
        WalkModeBtn.Clicked += OnWalkModeClicked;
        CarModeBtn.Clicked += OnCarModeClicked;
        ClearRouteBtn.Clicked += OnClearRouteClicked;
        BuildRouteBtn.Clicked += OnBuildRouteClicked;

        SetModeButtons();
    }

    // ---------- ОБНОВЛЕНИЕ СПИСКА ТОЧЕК ----------

    private void RefreshRoutePointsList()
    {
        RoutePointsContainer.Children.Clear();

        if (RoutePoints.Count == 0)
            return;

        for (int i = 0; i < RoutePoints.Count; i++)
        {
            int index = i;
            var p = RoutePoints[i];

            var frame = new Frame
            {
                Padding = 12,
                CornerRadius = 16,
                BackgroundColor = Colors.White,
                HasShadow = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var row = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },      // номер
                new ColumnDefinition { Width = GridLength.Star },      // название
                new ColumnDefinition { Width = GridLength.Auto }       // кнопки
            },
                VerticalOptions = LayoutOptions.Center
            };

            // --- Номер ---
            var indexLabel = new Label
            {
                Text = $"{index + 1}.",
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            row.Add(indexLabel);
            Grid.SetColumn(indexLabel, 0);

            // --- Название точки ---
            var nameLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(p.Name) ? "Точка маршрута" : p.Name,
                FontSize = 14,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.StartAndExpand
            };
            row.Add(nameLabel);
            Grid.SetColumn(nameLabel, 1);

            // --- Кнопки действий (↑ ↓ ✕) ---
            var actions = new HorizontalStackLayout
            {
                Spacing = 4,
                VerticalOptions = LayoutOptions.Center
            };

            // кнопка вверх
            var upBtn = new Button
            {
                Text = "↑",
                FontSize = 12,
                Padding = new Thickness(6, 2),
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Colors.Black,
                IsEnabled = index > 0
            };
            upBtn.Clicked += (_, __) => MoveRoutePoint(index, index - 1);
            actions.Children.Add(upBtn);

            // кнопка вниз
            var downBtn = new Button
            {
                Text = "↓",
                FontSize = 12,
                Padding = new Thickness(6, 2),
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                TextColor = Colors.Black,
                IsEnabled = index < RoutePoints.Count - 1
            };
            downBtn.Clicked += (_, __) => MoveRoutePoint(index, index + 1);
            actions.Children.Add(downBtn);

            // кнопка удалить
            var deleteBtn = new Button
            {
                Text = "✕",
                BackgroundColor = Color.FromArgb("#FEE2E2"),
                TextColor = Color.FromArgb("#B91C1C"),
                CornerRadius = 12,
                Padding = new Thickness(8, 2),
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            deleteBtn.Clicked += (_, __) =>
            {
                if (index >= 0 && index < RoutePoints.Count)
                {
                    RoutePoints.RemoveAt(index);
                    ClearRouteLineLayer();
                    ResetRouteInfo();
                    RefreshRoutePointsList();
                    UpdateMiniMapMarkers();
                }
            };
            actions.Children.Add(deleteBtn);

            row.Add(actions);
            Grid.SetColumn(actions, 2);

            frame.Content = row;
            RoutePointsContainer.Children.Add(frame);
        }
    }

    // ---------- МИНИ-КАРТА: МАРКЕРЫ И ЦЕНТР ----------

    private void UpdateMiniMapMarkers()
    {
        if (_miniMapControl.Map == null)
            return;

        var existingPointsLayer = _miniMapControl.Map.Layers
            .FirstOrDefault(l => l.Name == "RoutePointsLayer");

        if (existingPointsLayer != null)
            _miniMapControl.Map.Layers.Remove(existingPointsLayer);

        if (RoutePoints.Count == 0)
        {
            MiniMapPlaceholder.IsVisible = true;
            DistanceLabel.Text = "Расстояние: —";
            DurationLabel.Text = "Время: —";
            _miniMapControl.Refresh();
            return;
        }

        MiniMapPlaceholder.IsVisible = false;

        var features = new List<IFeature>();

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        bool hasBounds = false;

        foreach (var p in RoutePoints)
        {
            var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
            var point = new MPoint(x, y);

            var feature = new PointFeature(point);
            feature["name"] = p.Name ?? "";

            features.Add(feature);

            if (!hasBounds)
            {
                minX = maxX = point.X;
                minY = maxY = point.Y;
                hasBounds = true;
            }
            else
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }
        }

        var provider = new MemoryProvider(features);

        var style = new SymbolStyle
        {
            SymbolScale = 0.8,
            Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(0xFF, 0x25, 0x63, 0xEB)),
            Outline = new Pen(Mapsui.Styles.Color.White, 2)
        };

        var layer = new Layer("RoutePointsLayer")
        {
            DataSource = provider,
            Style = style
        };

        _miniMapControl.Map.Layers.Add(layer);

        if (hasBounds)
        {
            var box = new MRect(minX, minY, maxX, maxY);
            _miniMapControl.Map.Navigator.ZoomToBox(box, MBoxFit.Fit, 400);
        }

        _miniMapControl.Refresh();
    }
    private void ResetRouteInfo()
    {
        DistanceLabel.Text = "Расстояние: —";
        DurationLabel.Text = "Время: —";
    }

    private void MoveRoutePoint(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
            return;

        if (fromIndex < 0 || fromIndex >= RoutePoints.Count)
            return;

        if (toIndex < 0 || toIndex >= RoutePoints.Count)
            return;

        var item = RoutePoints[fromIndex];
        RoutePoints.RemoveAt(fromIndex);
        RoutePoints.Insert(toIndex, item);

        // порядок точек изменился → старый маршрут больше не валиден
        ClearRouteLineLayer();
        ResetRouteInfo();
        RefreshRoutePointsList();
        UpdateMiniMapMarkers();
    }

    // ---------- РЕЖИМ ПЕРЕДВИЖЕНИЯ ----------

    private void OnWalkModeClicked(object? sender, EventArgs e)
    {
        _currentMode = TravelMode.Walking;
        SetModeButtons();
    }

    private void OnCarModeClicked(object? sender, EventArgs e)
    {
        _currentMode = TravelMode.Driving;
        SetModeButtons();
    }

    private void SetModeButtons()
    {
        if (_currentMode == TravelMode.Walking)
        {
            WalkModeBtn.BackgroundColor = Color.FromArgb("#3B82F6");
            WalkModeBtn.TextColor = Colors.White;

            CarModeBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
            CarModeBtn.TextColor = Colors.Black;
        }
        else
        {
            CarModeBtn.BackgroundColor = Color.FromArgb("#3B82F6");
            CarModeBtn.TextColor = Colors.White;

            WalkModeBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
            WalkModeBtn.TextColor = Colors.Black;
        }
    }

    // ---------- КНОПКИ: ОЧИСТИТЬ / ПОСТРОИТЬ ----------

    private void OnClearRouteClicked(object? sender, EventArgs e)
    {
        RouteStore.Clear();
        RefreshRoutePointsList();
        ClearRouteLineLayer();
        ResetRouteInfo();
        UpdateMiniMapMarkers();
    }

    private void ClearRouteLineLayer()
    {
        if (_miniMapControl.Map == null) return;

        var lineLayer = _miniMapControl.Map.Layers
            .FirstOrDefault(l => l.Name == "RouteLineLayer");

        if (lineLayer != null)
            _miniMapControl.Map.Layers.Remove(lineLayer);

        _miniMapControl.Refresh();
    }

    private async void OnBuildRouteClicked(object? sender, EventArgs e)
    {
        if (RoutePoints.Count < 2)
        {
            await DisplayAlert("Маршрут", "Добавьте хотя бы две точки.", "OK");
            return;
        }

        await BuildRouteAsync();
    }

    // ---------- OSRM ----------

    private async Task BuildRouteAsync()
    {
        try
        {
            if (RoutePoints.Count < 2)
                throw new Exception("Добавьте хотя бы две точки.");

            // профиль OSRM: пешком / машина
            // для demo-сервера OSRM обычно доступны: driving, foot, bike
            var profile = _currentMode == TravelMode.Walking ? "foot" : "car";

            var coordString = string.Join(";",
                RoutePoints.Select(p =>
                    $"{p.Longitude.ToString(CultureInfo.InvariantCulture)}," +
                    $"{p.Latitude.ToString(CultureInfo.InvariantCulture)}"));

            var url = $"https://router.project-osrm.org/route/v1/{profile}/{coordString}" +
                      "?overview=full&geometries=geojson";

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // 1. HTTP запрос
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception(
                    $"OSRM вернул {(int)response.StatusCode} {response.ReasonPhrase}. " +
                    $"Тело ответа: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // 2. Разбираем JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // иногда OSRM кладёт в поле "code" значение "Ok" / "NoRoute"
            if (root.TryGetProperty("code", out var codeProp))
            {
                var code = codeProp.GetString();
                if (!string.Equals(code, "Ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"OSRM code = {code}");
                }
            }

            if (!root.TryGetProperty("routes", out var routesElement) ||
                routesElement.ValueKind != JsonValueKind.Array ||
                routesElement.GetArrayLength() == 0)
            {
                throw new Exception("Маршрут не найден.");
            }

            var route = routesElement[0];

            var distanceMeters = route.GetProperty("distance").GetDouble();
            var durationSeconds = route.GetProperty("duration").GetDouble();

            var km = distanceMeters / 1000.0;
            double minutes;

            // если режим "пешком" — считаем по средней скорости, например 5 км/ч
            if (_currentMode == TravelMode.Walking)
            {
                const double walkingSpeedKmH = 5.0;      // средняя скорость
                minutes = km / walkingSpeedKmH * 60.0;   // t = s / v
            }
            else
            {
                // для машины используем реальное время с OSRM
                minutes = durationSeconds / 60.0;
            }

            var modeLabel = _currentMode == TravelMode.Walking ? "пешком" : "на машине";

            DistanceLabel.Text = $"Расстояние ({modeLabel}): {km:F1} км";
            DurationLabel.Text = $"Время ({modeLabel}): {minutes:F0} мин";


            MiniMapPlaceholder.IsVisible = false;

            // 4. геометрия для линии маршрута
            var coords = new List<(double lon, double lat)>();

            if (route.TryGetProperty("geometry", out var geom) &&
                geom.TryGetProperty("coordinates", out var coordsArray) &&
                coordsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in coordsArray.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                    {
                        var lon = item[0].GetDouble();
                        var lat = item[1].GetDouble();
                        coords.Add((lon, lat));
                    }
                }
            }

            // 5. рисуем линию
            ClearRouteLineLayer();
            DrawRouteLine(coords);
        }
        catch (Exception ex)
        {
            ClearRouteLineLayer();
            await DisplayAlert("Маршрут", $"Не удалось построить маршрут: {ex.Message}", "OK");
        }
    }

    private void DrawRouteLine(IList<(double lon, double lat)> coords)
    {
        if (_miniMapControl.Map == null || coords.Count == 0)
            return;

        // убираем старую линию
        var existing = _miniMapControl.Map.Layers
            .FirstOrDefault(l => l.Name == "RouteLineLayer");
        if (existing != null)
            _miniMapControl.Map.Layers.Remove(existing);

        // переводим Lon/Lat в проекцию карты (x,y в EPSG:3857)
        var coordinates = coords
            .Select(c =>
            {
                var (x, y) = SphericalMercator.FromLonLat(c.lon, c.lat);
                return new Coordinate(x, y);
            })
            .ToArray();

        if (coordinates.Length < 2)
            return;

        // NTS-линия
        var lineString = new LineString(coordinates);

        // Feature для Mapsui (Nts)
        var feature = new GeometryFeature { Geometry = lineString };
        feature.Styles.Add(new VectorStyle
        {
            Line = new Pen(
                Mapsui.Styles.Color.FromArgb(0xFF, 0x25, 0x63, 0xEB), // синий
                4                                                     // толщина
            )
        });

        var provider = new MemoryProvider(feature);

        var layer = new Layer("RouteLineLayer")
        {
            DataSource = provider
        };

        _miniMapControl.Map.Layers.Add(layer);
        _miniMapControl.Refresh();
    }
}
