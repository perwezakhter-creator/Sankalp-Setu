using SankalpAPI.DTO;

namespace SankalpAPI.Interface
{
    public interface IFcmNotificationService
    {
        Task<ServiceResult<SendUserNotificationResponse>> SendToUserAsync(
            SendUserNotificationRequest request,
            CancellationToken cancellationToken = default);
    }
}
