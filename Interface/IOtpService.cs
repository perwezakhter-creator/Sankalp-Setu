using System.Threading.Tasks;
using SankalpAPI.DTO;
using SankalpAPI.Model;
using VerifyOtpRequest = SankalpAPI.DTO.VerifyOtpRequest;
using VerifyOtpResponse = SankalpAPI.DTO.VerifyOtpResponse;

namespace SankalpAPI.Interface
{
    public interface IOtpService
    {
        Task<OtpSendResponse> SendOtpAsync(SendOtpRequest request);
        Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request);
        Task<OtpVerifyResponse> VerifyOtpAsync(SmsVerifyOtpRequest request);
    }
}
