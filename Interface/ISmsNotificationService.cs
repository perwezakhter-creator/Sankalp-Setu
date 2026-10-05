using System.Threading.Tasks;

namespace SankalpAPI.Interface
{
    public interface ISmsNotificationService
    {
        Task<bool> SendOtpSmsAsync(string mobileNo, string otp);
        Task<(bool Success, string Message)> SendOtpAsync(string mobileNo, string otp);
        int GetOtpExpiryMinutes();
    }
}
