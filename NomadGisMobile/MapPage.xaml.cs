using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace NomadGisMobile;

public partial class MapPage : ContentPage
{
    private readonly MapControl _map;

    public MapPage()
    {
        InitializeComponent();

        _map = new MapControl
        {
            Map = new Mapsui.Map()
        };

        _map.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // просто показываем карту, никаких точек, никакого async
        Content = _map;
    }
}
