using System.Security.Cryptography;
using System.Text;

namespace SankalpAPI.Security
{
    public static class ClientTokenHasher
    {
        public static string Hash(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty.");

            byte[] bytes = Encoding.UTF8.GetBytes(token);

            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}
