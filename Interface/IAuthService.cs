using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
