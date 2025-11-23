namespace NomadGisMobile.Models
{
    public class LoginResponse
    {
        // названия свойств подгони под реальные из Swagger’а!
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
