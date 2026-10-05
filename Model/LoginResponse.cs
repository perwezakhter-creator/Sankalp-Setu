namespace SankalpAPI.Model
{
    public class LoginResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public string ClientToken { get; set; } = string.Empty;

        public UsrResponse? Data { get; set; }
    }

    public class UsrResponse
    {
        public string UserID { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        public string Mobile { get; set; } = string.Empty;
    }
}
