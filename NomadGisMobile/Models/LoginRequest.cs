namespace NomadGisMobile.Models
{
    public class LoginRequest
    {
        public string Identifier { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string Password { get; set; } = "";
    }

}
