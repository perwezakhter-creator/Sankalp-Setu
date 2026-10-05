using SankalpAPI.DTO;

namespace SankalpAPI.Interface
{
    public interface IAuthRepository
    {
        Task<UserDto?> LoginAsync(string userId, string password);
    }
}
