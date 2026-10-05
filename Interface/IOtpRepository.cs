using System.Threading.Tasks;

namespace SankalpAPI.Interface
{
    public interface IOtpRepository
    {
        Task<bool> SaveOtpAsync(string mobile, string otp, string? deviceId);
        Task<bool> ValidateOtpAsync(string mobile, string otp, int expiryMinutes);
    }
}
