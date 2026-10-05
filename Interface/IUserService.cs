using SankalpAPI.DTO;
using System.Data;

namespace SankalpAPI.Interface
{
    public interface IUserService
    {
        // Existing method preserved for backward compatibility
        DataTable InsertUser(string Username, string Password, string mobile);

        // Register user and map selected interests
        Task<ServiceResult<RegisterUserResponse>> RegisterUserAsync(
            RegisterUserRequest request,
            CancellationToken cancellationToken = default);
    }
}
