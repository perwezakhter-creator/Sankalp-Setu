using System.Text.Json.Serialization;

namespace SankalpAPI.Model
{
    public class LoginRequest
    {
        public string ClientKey { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
