using System.Security.Cryptography;

namespace SankalpAPI.Services
{
    public class ClientTokenService
    {
        public string GenerateToken()
        {
            byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
