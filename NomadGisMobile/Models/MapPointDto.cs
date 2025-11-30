namespace NomadGisMobile.Models
{
    public class MapPointDto
    {
        public string? Id { get; set; }          // или int?, если так в Swagger
        public string? Name { get; set; }
        public string? Description { get; set; } // чтобы было, откуда взять описание
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
