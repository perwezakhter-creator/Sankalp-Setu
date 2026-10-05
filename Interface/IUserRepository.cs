using Microsoft.Data.SqlClient;
using SankalpAPI.DTO;
using System.Data;

namespace SankalpAPI.Interface
{
    public interface IUserRepository
    {
        // Existing method preserved for backward compatibility
        DataTable InsertUser(string Username, string Password, string mobile);

        // Check if mobile number already exists in tbl_user
        Task<bool> IsMobileExistsAsync(
            string mobileNo,
            CancellationToken cancellationToken = default);

        // Atomic transaction: Check mobile, insert into tbl_user, retrieve identity User_id, insert interests into MapUser_Interest
        Task<(bool isSuccess, bool isDuplicate, long userId, string? errorMessage)> RegisterUserWithInterestsAsync(
            RegisterUserRequest request,
            CancellationToken cancellationToken = default);

        // Retrieve safe user data by mobile number from tbl_user
        Task<UserData?> GetUserByMobileAsync(
            string mobileNo,
            CancellationToken cancellationToken = default);

        // Retrieve existing DeviceId for a user by mobile number from tbl_user
        Task<string?> GetDeviceIdByMobileAsync(
            string mobileNo,
            CancellationToken cancellationToken = default);

        // Update existing user's DeviceId in tbl_user
        Task<bool> UpdateUserDeviceIdAsync(string mobileNo, string deviceId, CancellationToken cancellationToken = default);
    }
}
