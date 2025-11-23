namespace NomadGisMobile.Models
{
    public class MapPointDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int UnlockRadiusMeters { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
