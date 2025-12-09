using System.Collections.ObjectModel;

namespace NomadGisMobile.Routing
{
    public class RoutePoint
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public static class RouteStore
    {
        public static ObservableCollection<RoutePoint> Points { get; } = new();

        public static void AddPoint(string id, string name, double lat, double lon)
        {
            Points.Add(new RoutePoint
            {
                Id = id ?? "",
                Name = string.IsNullOrWhiteSpace(name) ? "Точка" : name,
                Latitude = lat,
                Longitude = lon
            });
        }

        public static void Clear() => Points.Clear();
    }
}
