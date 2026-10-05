using System.Collections.Generic;
using System.Threading.Tasks;
using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IApplicantRepository
    {
        /// <summary>
        /// Saves or updates beneficiary/applicant records returned from external portal into the database.
        /// </summary>
        Task<bool> SaveBeneficiaryDetailsAsync(long portalId, string mobileNo,string? deviceId, string? session, string? userType, List<BeneficiaryData> beneficiaries, string? rawJson = null);

        /// <summary>
        /// Validates OTP against tbl_otp and tbl_beneficiary_details.
        /// </summary>
        Task<(bool IsValid, string? UserId)> VerifyOtpAsync(long portalId, string mobileNo, string? deviceId, string otp, int expiryMinutes = 10, long? aId = null);

        /// <summary>
        /// Saves secret key and token in tbl_map_user_portal and registers token in tbl_client_token.
        /// </summary>
        Task<bool> SaveSecretAndTokenAsync(long portalId, string mobileNo, string? deviceId, string? userId, string secretKey, string token, string? tokenHash);

        /// <summary>
        /// Retrieves the latest beneficiary record information (a_id, session, device_id, beneficiary_number)
        /// previously saved for the given portal and mobile number.
        /// </summary>
        Task<(long A_Id, string? Session, string? DeviceId, string? BeneficiaryNumber)> GetLatestBeneficiaryInfoAsync(long portalId, string mobileNo, long? aId = null);

        /// <summary>
        /// Marks OTP as validated in tbl_otp.
        /// </summary>
        Task<bool> MarkOtpValidatedAsync(string mobileNo, string otp);

        /// <summary>
        /// Updates the verification status (is_verified) in tbl_beneficiary_details for the given a_id.
        /// </summary>
        Task<bool> UpdateBeneficiaryVerificationStatusAsync(long aId, string isVerified = "Y", string? DeviceId = null);

        /// <summary>
        /// Saves or updates a beneficiary message in tbl_Message (with mobile) and logs its sync status in tbl_Message_log.
        /// </summary>
        Task<(long MsgId, long MsgLogId)> SaveBeneficiaryMessageAsync(long portalId, string mobileNo, string? deviceId, BeneficiaryMessageItem message);

        /// <summary>
        /// Retrieves the authorization token for a user from tbl_map_user_portal.
        /// </summary>
        Task<string?> GetUserTokenAsync(long portalId, string mobileNo);

        /// <summary>
        /// Resolves portal ID, mobile number, and device ID associated with a user token.
        /// </summary>
        Task<(long PortalId, string? Mobile, string? DeviceId)> GetUserByTokenAsync(string token);
    }
}
