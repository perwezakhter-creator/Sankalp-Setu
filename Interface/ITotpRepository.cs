using System.Threading.Tasks;

namespace SankalpAPI.Interface
{
    public class UserExistenceInfo
    {
        public bool Exists { get; set; }
        public string? Name { get; set; }
        public string? UserId { get; set; }
        public string Mobile { get; set; } = string.Empty;
    }

    public interface ITotpRepository
    {
        Task<UserExistenceInfo> CheckUserExistsAsync(long portalId, string mobile, string? userId);
        Task<bool> SaveOtpAsync(string mobile, string otp, string? deviceId);
        Task<bool> ValidateOtpAsync(string mobile, string otp, int expiryMinutes);
        Task<bool> SaveTotpSecretAsync(long portalId, string mobile, string? userId, string secretKey, string? token, string entryBy);
        Task<string?> GetTotpSecretAsync(long portalId, string mobile);
    }
}
